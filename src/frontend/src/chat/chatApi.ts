import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'

export type ChatRoom = { id:string; transactionId:string; statusCode:string; counterpartyRoleCode:string; counterpartyDisplayName:string; serviceName:string; transactionNumber:string; lastMessagePreview:string|null; lastMessageAt:string|null; unreadCount:number; rowVersion?:string }
export type ChatAttachment = { id:string; fileName:string; contentType:string; sizeBytes:number; available:boolean; downloadUrl:string|null; publicationStatus:string }
export type ChatMessage = { id:string; messageTypeCode:'TEXT'|'FILE'; body:string|null; senderRoleCode:string; senderDisplayName:string; isMine:boolean; createdAt:string; isReadByCounterparty:boolean; attachment:ChatAttachment|null; rowVersion:string }
export type ChatMessagePage = { items:ChatMessage[]; nextCursor:string|null; hasMore:boolean }

async function request<T>(path:string, init?:RequestInit):Promise<T>{
  const response=await fetch(path,{credentials:'include',headers:init?.body instanceof FormData?undefined:{'Content-Type':'application/json',...(init?.headers??{})},...init})
  if(!response.ok){const body=await response.json().catch(()=>null) as {message?:string}|null;throw new Error(body?.message??'채팅 요청을 처리하지 못했습니다.')}
  if(response.status===204)return undefined as T
  return response.json() as Promise<T>
}

export const chatApi={
  rooms:()=>request<ChatRoom[]>('/api/v1/chat/rooms'),
  room:(id:string)=>request<ChatRoom>(`/api/v1/chat/rooms/${id}`),
  transactionRoom:(id:string)=>request<ChatRoom>(`/api/v1/chat/transactions/${id}/room`),
  messages:(id:string,before?:string)=>request<ChatMessagePage>(`/api/v1/chat/rooms/${id}/messages?pageSize=30${before?`&before=${before}`:''}`),
  sendText:(id:string,body:string)=>request<ChatMessage>(`/api/v1/chat/rooms/${id}/messages/text`,{method:'POST',body:JSON.stringify({body,idempotencyKey:crypto.randomUUID()})}),
  sendFile:(id:string,file:File)=>{const form=new FormData();form.append('file',file);form.append('idempotencyKey',crypto.randomUUID());return request<ChatMessage>(`/api/v1/chat/rooms/${id}/messages/file`,{method:'POST',body:form})},
  read:(id:string,messageId:string)=>request<void>(`/api/v1/chat/rooms/${id}/read`,{method:'POST',body:JSON.stringify({messageId})}),
  unread:()=>request<{count:number}>('/api/v1/chat/unread-count'),
}

export function connectChat(roomId:string,onMessage:(message:ChatMessage)=>void,onRead:()=>void):HubConnection{
  const connection=new HubConnectionBuilder().withUrl('/hubs/chat').withAutomaticReconnect([0,2000,5000,10000]).configureLogging(LogLevel.Warning).build()
  connection.on('messageCreated',onMessage)
  connection.on('messagesRead',onRead)
  connection.onreconnected(()=>connection.invoke('JoinRoom',roomId).catch(()=>undefined))
  void connection.start().then(()=>connection.invoke('JoinRoom',roomId)).catch(()=>undefined)
  return connection
}

export async function disconnectChat(connection:HubConnection|undefined,roomId:string){
  if(!connection)return
  if(connection.state===HubConnectionState.Connected)await connection.invoke('LeaveRoom',roomId).catch(()=>undefined)
  await connection.stop().catch(()=>undefined)
}
