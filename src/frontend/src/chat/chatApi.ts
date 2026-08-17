import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { apiUrl } from '../config/apiEndpoint'

export type ChatResourceType='TRANSACTION'|'SUBSCRIPTION'|'INTERIOR'|'AFTER_SERVICE'
export type ChatRoom = { id:string; resourceTypeCode:ChatResourceType; resourceId:string; transactionId:string|null; roomTypeCode:string; statusCode:string; counterpartyRoleCode:string; counterpartyDisplayName:string; serviceName:string; resourceNumber:string; lastMessagePreview:string|null; lastMessageAt:string|null; unreadCount:number; rowVersion?:string }
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
  rooms:(resourceType?:ChatResourceType,page=1)=>request<ChatRoom[]>(`/api/v1/chat/rooms?page=${page}&pageSize=30${resourceType?`&resourceType=${resourceType}`:''}`),
  room:(id:string)=>request<ChatRoom>(`/api/v1/chat/rooms/${id}`),
  transactionRoom:(id:string)=>request<ChatRoom>(`/api/v1/chat/transactions/${id}/room`),
  subscriptionRoom:(id:string)=>request<ChatRoom>(`/api/v1/chat/subscriptions/${id}/room`),
  subscriptionVisitRoom:(id:string)=>request<ChatRoom>(`/api/v1/chat/subscription-visits/${id}/room`),
  interiorRoom:(id:string,role:'PRIMARY_CONTRACTOR'|'SITE_SURVEY'='PRIMARY_CONTRACTOR')=>request<ChatRoom>(`/api/v1/chat/interiors/${id}/room?role=${role}`),
  afterServiceRoom:(id:string)=>request<ChatRoom>(`/api/v1/chat/after-services/${id}/room`),
  messages:(id:string,before?:string)=>request<ChatMessagePage>(`/api/v1/chat/rooms/${id}/messages?pageSize=30${before?`&before=${before}`:''}`),
  sendText:(id:string,body:string)=>request<ChatMessage>(`/api/v1/chat/rooms/${id}/messages/text`,{method:'POST',body:JSON.stringify({body,idempotencyKey:crypto.randomUUID()})}),
  sendFile:(id:string,file:File)=>{const form=new FormData();form.append('file',file);form.append('idempotencyKey',crypto.randomUUID());return request<ChatMessage>(`/api/v1/chat/rooms/${id}/messages/file`,{method:'POST',body:form})},
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

export function connectChat(roomId:string,onMessage:(message:ChatMessage)=>void,onRead:()=>void):HubConnection{
  const connection=new HubConnectionBuilder().withUrl(apiUrl('/hubs/chat'),{withCredentials:true}).withAutomaticReconnect([0,2000,5000,10000]).configureLogging(LogLevel.Warning).build()
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
