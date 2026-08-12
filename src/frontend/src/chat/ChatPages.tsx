import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { navigate } from '../auth/routing'
import { CustomerAppLayout } from '../customer/CustomerAppLayout'
import { AuthenticatedLayout } from '../components/AuthenticatedLayout'
import { chatApi, connectChat, disconnectChat, type ChatMessage, type ChatRoom } from './chatApi'
import './chat.css'

type Audience='customer'|'provider'
const base=(audience:Audience)=>audience==='customer'?'/customer/messages':'/provider/messages'

export function ChatRoomListPage({audience}:{audience:Audience}){
  const[rooms,setRooms]=useState<ChatRoom[]>([]),[error,setError]=useState('')
  useEffect(()=>{chatApi.rooms().then(setRooms).catch((reason:Error)=>setError(reason.message))},[])
  const content=<><section className="chatHeading"><p>SOODAL TALK</p><h1>메시지</h1><span>견적 채택 후 생성된 거래의 고객과 선택 공급자만 대화할 수 있습니다.</span></section>{error&&<p className="chatError">{error}</p>}<section className="chatRoomList">{rooms.map(room=><button key={room.id} onClick={()=>navigate(`${base(audience)}/${room.id}`)}><div className="chatAvatar" aria-hidden="true">{room.counterpartyDisplayName.slice(0,1)}</div><div><span>{room.transactionNumber} · {room.serviceName}</span><strong>{room.counterpartyDisplayName}</strong><p>{room.lastMessagePreview??'거래 채팅을 시작해 보세요.'}</p></div><aside>{room.lastMessageAt&&<time>{shortDate(room.lastMessageAt)}</time>}{room.unreadCount>0&&<b>{room.unreadCount>99?'99+':room.unreadCount}</b>}</aside></button>)}{rooms.length===0&&!error&&<div className="chatEmpty"><strong>아직 거래 채팅이 없습니다.</strong><p>견적을 채택해 거래가 생성되면 안전한 1:1 채팅을 이용할 수 있습니다.</p></div>}</section></>
  return audience==='customer'?<CustomerAppLayout>{content}</CustomerAppLayout>:<AuthenticatedLayout>{content}</AuthenticatedLayout>
}

export function ChatRoomPage({audience,id}:{audience:Audience;id:string}){
  const[room,setRoom]=useState<ChatRoom|null>(null),[messages,setMessages]=useState<ChatMessage[]>([]),[cursor,setCursor]=useState<string|null>(null),[hasMore,setHasMore]=useState(false),[body,setBody]=useState(''),[file,setFile]=useState<File|null>(null),[error,setError]=useState(''),[sending,setSending]=useState(false)
  const connection=useRef<HubConnection|undefined>(undefined),end=useRef<HTMLDivElement>(null)
  const merge=useCallback((incoming:ChatMessage[])=>setMessages(current=>{const map=new Map(current.map(value=>[value.id,value]));incoming.forEach(value=>map.set(value.id,value));return [...map.values()].sort((a,b)=>new Date(a.createdAt).getTime()-new Date(b.createdAt).getTime())}),[])
  const load=useCallback(async()=>{const[detail,page]=await Promise.all([chatApi.room(id),chatApi.messages(id)]);setRoom(detail);setMessages(page.items);setCursor(page.nextCursor);setHasMore(page.hasMore);const last=page.items.at(-1);if(last)await chatApi.read(id,last.id)},[id])
  useEffect(()=>{load().catch((reason:Error)=>setError(reason.message))},[load])
  useEffect(()=>{connection.current=connectChat(id,message=>{merge([message]);if(!message.isMine)void chatApi.read(id,message.id)},()=>void load());const poll=window.setInterval(()=>void load().catch(()=>undefined),15000);return()=>{window.clearInterval(poll);void disconnectChat(connection.current,id)}},[id,load,merge])
  useEffect(()=>{end.current?.scrollIntoView({behavior:'smooth'})},[messages.length])
  const older=async()=>{if(!cursor)return;try{const page=await chatApi.messages(id,cursor);merge(page.items);setCursor(page.nextCursor);setHasMore(page.hasMore)}catch(reason){setError((reason as Error).message)}}
  const send=async(event:FormEvent)=>{event.preventDefault();if(!body.trim()||sending)return;try{setSending(true);merge([await chatApi.sendText(id,body)]);setBody('')}catch(reason){setError((reason as Error).message)}finally{setSending(false)}}
  const attach=async()=>{if(!file||sending)return;try{setSending(true);merge([await chatApi.sendFile(id,file)]);setFile(null)}catch(reason){setError((reason as Error).message)}finally{setSending(false)}}
  const content=<div className="chatPage"><header className="chatRoomHeader"><button onClick={()=>navigate(base(audience))} aria-label="메시지 목록으로">←</button><div><strong>{room?.counterpartyDisplayName??'채팅'}</strong><span>{room?.transactionNumber} · {room?.serviceName}</span></div><b className={`roomStatus status-${room?.statusCode}`}>{room?.statusCode==='ACTIVE'?'대화 가능':room?.statusCode==='READ_ONLY'?'읽기 전용':'종료'}</b></header>{error&&<p className="chatError">{error}</p>}<main className="chatTimeline">{hasMore&&<button className="loadOlder" onClick={()=>void older()}>이전 메시지 보기</button>}{messages.map(message=><article className={message.isMine?'mine':'theirs'} key={message.id}><small>{message.senderDisplayName}</small><div>{message.messageTypeCode==='TEXT'?<p>{message.body}</p>:<Attachment message={message}/>}</div><footer><time>{time(message.createdAt)}</time>{message.isMine&&<span>{message.isReadByCounterparty?'읽음':'전송됨'}</span>}</footer></article>)}{messages.length===0&&<div className="chatEmpty"><strong>안전한 거래 채팅이 시작되었습니다.</strong><p>전화번호와 상세주소는 채팅이 아닌 기존 거래 개인정보 공개정책에 따라 확인해 주세요.</p></div>}<div ref={end}/></main><footer className="chatComposer"><div className="chatFileRow"><label>파일 첨부<input type="file" accept="image/jpeg,image/png,application/pdf" onChange={event=>setFile(event.target.files?.[0]??null)}/></label>{file&&<><span>{file.name}</span><button disabled={sending} onClick={()=>void attach()}>보내기</button></>}</div><form onSubmit={event=>void send(event)}><textarea aria-label="메시지" maxLength={4000} rows={1} placeholder={room?.statusCode==='ACTIVE'?'메시지를 입력하세요.':'새 메시지를 보낼 수 없습니다.'} disabled={room?.statusCode!=='ACTIVE'} value={body} onChange={event=>setBody(event.target.value)}/><button disabled={room?.statusCode!=='ACTIVE'||!body.trim()||sending}>전송</button></form><small>첨부파일은 안전검사 완료 전 상대방에게 공개되지 않습니다.</small></footer></div>
  return audience==='customer'?<CustomerAppLayout>{content}</CustomerAppLayout>:<AuthenticatedLayout>{content}</AuthenticatedLayout>
}

function Attachment({message}:{message:ChatMessage}){const value=message.attachment;if(!value)return <p>첨부파일 정보를 확인할 수 없습니다.</p>;return <div className="chatAttachment"><strong>{value.fileName}</strong><span>{formatSize(value.sizeBytes)} · {value.contentType}</span>{value.available&&value.downloadUrl?<a href={value.downloadUrl}>파일 열기</a>:<em>안전검사 완료 후 상대방에게 공개됩니다.</em>}</div>}
const formatSize=(value:number)=>value<1024?`${value} B`:value<1024*1024?`${Math.round(value/1024)} KB`:`${(value/1024/1024).toFixed(1)} MB`
const time=(value:string)=>new Intl.DateTimeFormat('ko-KR',{hour:'2-digit',minute:'2-digit'}).format(new Date(value))
const shortDate=(value:string)=>new Intl.DateTimeFormat('ko-KR',{month:'numeric',day:'numeric'}).format(new Date(value))
