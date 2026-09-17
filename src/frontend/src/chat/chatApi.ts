import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { apiUrl } from '../config/apiEndpoint'

export type ChatResourceType='TRANSACTION'|'SUBSCRIPTION'|'INTERIOR'|'AFTER_SERVICE'|'PROVIDER_CONSULTATION'
export type ChatRoom = { id:string; resourceTypeCode:ChatResourceType; resourceId:string; transactionId:string|null; roomTypeCode:string; statusCode:string; counterpartyRoleCode:string; counterpartyDisplayName:string; serviceName:string; resourceNumber:string; lastMessagePreview:string|null; lastMessageAt:string|null; unreadCount:number; rowVersion?:string }
export type ChatAttachment = { id:string; fileName:string; contentType:string; sizeBytes:number; available:boolean; downloadUrl:string|null; publicationStatus:string }
export type ChatMessage = { id:string; messageTypeCode:'TEXT'|'FILE'; body:string|null; senderRoleCode:string; senderDisplayName:string; isMine:boolean; createdAt:string; isReadByCounterparty:boolean; attachment:ChatAttachment|null; rowVersion:string }
export type ChatMessagePage = { items:ChatMessage[]; nextCursor:string|null; hasMore:boolean }
const intentionallyStoppedConnections=new WeakSet<HubConnection>()
const reconnectDelay=(milliseconds:number)=>new Promise(resolve=>window.setTimeout(resolve,milliseconds))

async function request<T>(path:string, init?:RequestInit):Promise<T>{
  let response:Response
  try{
    response=await fetch(apiUrl(path),{credentials:'include',headers:init?.body instanceof FormData?undefined:{'Content-Type':'application/json',...(init?.headers??{})},...init})
  }catch{
    throw new Error('서버에 연결하지 못했습니다. 네트워크 상태를 확인한 후 다시 시도해 주세요.')
  }
  if(!response.ok){const body=await response.json().catch(()=>null) as {message?:string;traceId?:string}|null;const suffix=body?.traceId&&!body.message?.includes(body.traceId)?` 오류번호: ${body.traceId}`:'';throw new Error(`${body?.message??`채팅 요청을 처리하지 못했습니다. (HTTP ${response.status})`}${suffix}`)}
  if(response.status===204)return undefined as T
  return response.json() as Promise<T>
}

async function sendImage(id:string,file:File):Promise<ChatMessage>{
  const form=new FormData();form.append('file',file);form.append('idempotencyKey',crypto.randomUUID())
  return request<ChatMessage>(`/api/v1/chat/rooms/${id}/messages/file`,{method:'POST',body:form})
}

export const chatApi={
  rooms:(resourceType?:ChatResourceType,page=1)=>request<ChatRoom[]>(`/api/v1/chat/rooms?page=${page}&pageSize=30${resourceType?`&resourceType=${resourceType}`:''}`),
  room:(id:string)=>request<ChatRoom>(`/api/v1/chat/rooms/${id}`),
  transactionRoom:(id:string)=>request<ChatRoom>(`/api/v1/chat/transactions/${id}/room`),
  subscriptionRoom:(id:string)=>request<ChatRoom>(`/api/v1/chat/subscriptions/${id}/room`),
  subscriptionVisitRoom:(id:string)=>request<ChatRoom>(`/api/v1/chat/subscription-visits/${id}/room`),
  interiorRoom:(id:string,role:'PRIMARY_CONTRACTOR'|'SITE_SURVEY'='PRIMARY_CONTRACTOR')=>request<ChatRoom>(`/api/v1/chat/interiors/${id}/room?role=${role}`),
  afterServiceRoom:(id:string)=>request<ChatRoom>(`/api/v1/chat/after-services/${id}/room`),
  providerConsultationRoom:(providerId:string)=>request<ChatRoom>(`/api/v1/chat/provider-consultations/${providerId}/room`,{method:'POST'}),
  messages:(id:string,before?:string)=>request<ChatMessagePage>(`/api/v1/chat/rooms/${id}/messages?pageSize=30${before?`&before=${before}`:''}`),
  sendText:(id:string,body:string)=>request<ChatMessage>(`/api/v1/chat/rooms/${id}/messages/text`,{method:'POST',body:JSON.stringify({body,idempotencyKey:crypto.randomUUID()})}),
  sendFile:sendImage,
  read:(id:string,messageId:string)=>request<void>(`/api/v1/chat/rooms/${id}/read`,{method:'POST',body:JSON.stringify({messageId})}),
  unread:()=>request<{count:number}>('/api/v1/chat/unread-count'),
}

export async function openBusinessChat(audience:'customer'|'provider',resourceType:ChatResourceType,resourceId:string,roomType?:'PRIMARY_CONTRACTOR'|'SITE_SURVEY'){
  const room=resourceType==='TRANSACTION'?await chatApi.transactionRoom(resourceId)
    :resourceType==='SUBSCRIPTION'?await chatApi.subscriptionRoom(resourceId)
    :resourceType==='INTERIOR'?await chatApi.interiorRoom(resourceId,roomType)
    :await chatApi.afterServiceRoom(resourceId)
  window.history.pushState({},'',`/${audience}/messages/${room.id}`)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

export async function openSubscriptionVisitChat(audience:'customer'|'provider',visitId:string){
  const room=await chatApi.subscriptionVisitRoom(visitId)
  window.history.pushState({},'',`/${audience}/messages/${room.id}`)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

export function connectChat(roomId:string,onMessage:(message:ChatMessage)=>void,onRead:()=>void,onConnectionState?:(connected:boolean)=>void):HubConnection{
  const connection=new HubConnectionBuilder().withUrl(apiUrl('/hubs/chat'),{withCredentials:true}).withAutomaticReconnect([0,2000,5000,10000]).configureLogging(LogLevel.Warning).build()
  connection.on('messageCreated',onMessage)
  connection.on('messagesRead',onRead)
  connection.onreconnecting(()=>onConnectionState?.(false))
  connection.onreconnected(async()=>{try{await connection.invoke('JoinRoom',roomId);onConnectionState?.(true)}catch{onConnectionState?.(false)}})
  const start=async()=>{
    while(!intentionallyStoppedConnections.has(connection)&&connection.state===HubConnectionState.Disconnected){
      try{await connection.start();await connection.invoke('JoinRoom',roomId);onConnectionState?.(true);return}
      catch{onConnectionState?.(false);if(!intentionallyStoppedConnections.has(connection))await reconnectDelay(1500)}
    }
  }
  connection.onclose(()=>{onConnectionState?.(false);if(!intentionallyStoppedConnections.has(connection))void start()})
  void start()
  return connection
}

export async function disconnectChat(connection:HubConnection|undefined,roomId:string){
  if(!connection)return
  intentionallyStoppedConnections.add(connection)
  if(connection.state===HubConnectionState.Connected)await connection.invoke('LeaveRoom',roomId).catch(()=>undefined)
  await connection.stop().catch(()=>undefined)
}
