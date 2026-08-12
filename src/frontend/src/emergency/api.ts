import type {EmergencyAvailability,EmergencyCustomerResponse,EmergencyProgress,EmergencyProviderAssignment,EmergencyProviderRequest,EmergencySelection} from './types'
type ApiError={message?:string}
async function json<T>(r:Response):Promise<T>{if(!r.ok){let b:ApiError={};try{b=await r.json() as ApiError}catch{/* no body */}throw new Error(b.message??'긴급출동 요청을 처리하지 못했습니다.')}return r.json() as Promise<T>}
const get=<T,>(p:string)=>fetch(p,{credentials:'include'}).then(json<T>)
const send=<T,>(m:string,p:string,b:unknown)=>fetch(p,{method:m,credentials:'include',headers:{'Content-Type':'application/json'},body:JSON.stringify(b)}).then(json<T>)
export const availability=()=>get<EmergencyAvailability>('/api/v1/providers/me/emergency-availability')
export const saveAvailability=(body:unknown)=>send<EmergencyAvailability>('PUT','/api/v1/providers/me/emergency-availability',body)
export const providerRequests=()=>get<EmergencyProviderRequest[]>('/api/v1/providers/me/emergency-requests')
export const providerAssignments=()=>get<EmergencyProviderAssignment[]>('/api/v1/providers/me/emergency-requests/assignments')
export const respond=(requestId:string,body:unknown)=>send('POST',`/api/v1/emergency-requests/${requestId}/responses`,body)
export const customerResponses=(requestId:string)=>get<EmergencyCustomerResponse[]>(`/api/v1/emergency-requests/${requestId}/responses`)
export const selectProvider=(requestId:string,body:unknown)=>send<EmergencySelection>('POST',`/api/v1/emergency-requests/${requestId}/select`,body)
export const progress=(transactionId:string)=>get<EmergencyProgress>(`/api/v1/emergency-transactions/${transactionId}/progress`)
export const addProgress=(transactionId:string,eventType:string)=>send<EmergencyProgress>('POST',`/api/v1/emergency-transactions/${transactionId}/progress`,{eventType,note:null,idempotencyKey:crypto.randomUUID()})
