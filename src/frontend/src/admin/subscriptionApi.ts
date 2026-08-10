import type { CareProduct,CreateCareProductInput,SubscriptionApplication,SubscriptionContract,SubscriptionRequest,SubscriptionScheduleChange,SubscriptionService,SubscriptionVisit } from './subscriptionTypes'

interface ApiError { message?:string }
async function request<T>(path:string,init?:RequestInit):Promise<T>{const response=await fetch(path,{credentials:'include',...init,headers:init?.body?{'Content-Type':'application/json',...init.headers}:init?.headers});if(!response.ok){let message='요청을 처리하지 못했습니다.';try{const body=await response.json() as ApiError;if(body.message)message=body.message}catch{/* 공통 안내를 사용합니다. */}throw new Error(message)}return response.json() as Promise<T>}
export const getSubscriptionServices=()=>request<SubscriptionService[]>('/api/v1/admin/subscriptions/eligible-services')
export const getCareProducts=()=>request<CareProduct[]>('/api/v1/admin/subscriptions/products')
export const createCareProduct=(input:CreateCareProductInput)=>request<CareProduct>('/api/v1/admin/subscriptions/products',{method:'POST',body:JSON.stringify(input)})
export const getSubscriptionRequests=()=>request<SubscriptionRequest[]>('/api/v1/admin/subscriptions/requests')
export const getSubscriptionApplications=()=>request<SubscriptionApplication[]>('/api/v1/admin/subscriptions/applications')
export const getSubscriptionContracts=()=>request<SubscriptionContract[]>('/api/v1/admin/subscriptions/contracts')
export const getSubscriptionVisits=(status='')=>request<SubscriptionVisit[]>(`/api/v1/admin/subscriptions/visits${status?`?status=${encodeURIComponent(status)}`:''}`)
export const getSubscriptionScheduleChanges=()=>request<SubscriptionScheduleChange[]>('/api/v1/admin/subscriptions/schedule-changes')
export const decideSubscriptionScheduleChange=(id:string,approve:boolean)=>request<SubscriptionScheduleChange>(`/api/v1/admin/subscriptions/schedule-changes/${id}/decision`,{method:'POST',body:JSON.stringify({approve,idempotencyKey:`admin-schedule-${id}-${crypto.randomUUID()}`,rowVersion:null})})
export const changeContractState=(id:string,action:'pause'|'resume'|'terminate',reason:string)=>request<SubscriptionContract>(`/api/v1/admin/subscriptions/contracts/${id}/${action}`,{method:'POST',body:JSON.stringify({reason,resumePlannedAt:null,idempotencyKey:`admin-${action}-${id}-${crypto.randomUUID()}`,rowVersion:null})})
export const replaceSubscriptionProvider=(id:string,providerId:string,reason:string)=>request<SubscriptionContract>(`/api/v1/admin/subscriptions/contracts/${id}/provider`,{method:'POST',body:JSON.stringify({providerId,reason,idempotencyKey:`admin-provider-${id}-${crypto.randomUUID()}`,rowVersion:null})})
export const skipSubscriptionVisit=(id:string)=>request<SubscriptionVisit>(`/api/v1/admin/subscriptions/visits/${id}/skip`,{method:'POST',body:JSON.stringify({idempotencyKey:`admin-skip-${id}-${crypto.randomUUID()}`})})
