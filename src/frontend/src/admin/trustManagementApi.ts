import type { AdminTrustDetail,AdminTrustList,TrustCalculation,TrustPolicy } from './trustManagementTypes'

interface ApiError { message?:string }
async function request<T>(path:string):Promise<T>{const response=await fetch(path,{credentials:'include'});if(!response.ok){let message='신뢰도 정보를 불러오지 못했습니다.';try{const body=await response.json() as ApiError;if(body.message)message=body.message}catch{/* 공통 안내 */}throw new Error(message)}return response.json() as Promise<T>}
async function send<T>(path:string,method:string,value:object):Promise<T>{const response=await fetch(path,{method,credentials:'include',headers:{'Content-Type':'application/json'},body:JSON.stringify(value)});if(!response.ok){let message='신뢰도 정책 업무를 처리하지 못했습니다.';try{const body=await response.json() as ApiError;if(body.message)message=body.message}catch{/* 공통 안내 */}throw new Error(message)}return response.json() as Promise<T>}
function query(values:Record<string,string|number|boolean>){const result=new URLSearchParams();Object.entries(values).forEach(([key,value])=>{if(value!=='')result.set(key,String(value))});return result.toString()}
export const searchAdminTrust=(values:Record<string,string|number|boolean>)=>request<AdminTrustList>(`/api/v1/admin/trust?${query(values)}`)
export const getAdminTrust=(providerId:string)=>request<AdminTrustDetail>(`/api/v1/admin/trust/${providerId}`)
export const getTrustPolicies=()=>request<TrustPolicy[]>('/api/v1/admin/trust/policies')
export const updateTrustPolicy=(id:string,value:object)=>send<TrustPolicy>(`/api/v1/admin/trust/policies/${id}`,'PUT',value)
export const cloneTrustPolicy=(id:string,value:object)=>send<TrustPolicy>(`/api/v1/admin/trust/policies/${id}/clone`,'POST',value)
export const approveTrustPolicy=(id:string,value:object)=>send<TrustPolicy>(`/api/v1/admin/trust/policies/${id}/approve`,'POST',value)
export const activateTrustPolicy=(id:string,value:object)=>send<TrustPolicy>(`/api/v1/admin/trust/policies/${id}/activate`,'POST',value)
export const retireTrustPolicy=(id:string,value:object)=>send<TrustPolicy>(`/api/v1/admin/trust/policies/${id}/retire`,'POST',value)
export const simulateTrust=(providerId:string,policyId:string)=>send<TrustCalculation>(`/api/v1/admin/trust/${providerId}/simulation`,'POST',{policyId,idempotencyKey:crypto.randomUUID()})
