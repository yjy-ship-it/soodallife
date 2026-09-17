import { apiUrl } from '../config/apiEndpoint'
import type { AdminTrustDetail,AdminTrustList,TrustCalculation,TrustManualAdjustment,TrustPolicy,TrustReferenceData } from './trustManagementTypes'

interface ApiError { message?:string }
async function request<T>(path:string):Promise<T>{const response=await fetch(path,{credentials:'include'});if(!response.ok){let message='신뢰도 정보를 불러오지 못했습니다.';try{const body=await response.json() as ApiError;if(body.message)message=body.message}catch{/* 공통 안내 */}throw new Error(message)}return response.json() as Promise<T>}
async function send<T>(path:string,method:string,value:object):Promise<T>{const controller=new AbortController();const timeout=window.setTimeout(()=>controller.abort(),30000);let response:Response;try{response=await fetch(apiUrl(path),{method,credentials:'include',cache:'no-store',signal:controller.signal,headers:{'Content-Type':'application/json','Accept':'application/json'},body:JSON.stringify(value)})}catch(reason){throw new Error(reason instanceof DOMException&&reason.name==='AbortError'?'서버 응답 시간이 초과되었습니다. API 서버 상태를 확인해 주세요.':'신뢰도 정책 API에 연결하지 못했습니다. 새로고침 후 다시 시도해 주세요.')}finally{window.clearTimeout(timeout)}if(!response.ok){let message=`신뢰도 정책 업무를 처리하지 못했습니다. (${response.status})`;try{const body=await response.json() as ApiError;if(body.message)message=body.message}catch{/* 공통 안내 */}throw new Error(message)}return response.json() as Promise<T>}
function query(values:Record<string,string|number|boolean>){const result=new URLSearchParams();Object.entries(values).forEach(([key,value])=>{if(value!=='')result.set(key,String(value))});return result.toString()}
export const searchAdminTrust=(values:Record<string,string|number|boolean>)=>request<AdminTrustList>(`/api/v1/admin/trust?${query(values)}`)
export const getAdminTrust=(providerId:string)=>request<AdminTrustDetail>(`/api/v1/admin/trust/${providerId}`)
export const getTrustPolicies=()=>request<TrustPolicy[]>('/api/v1/admin/trust/policies')
export const updateTrustPolicy=(id:string,value:object)=>send<TrustPolicy>(`/api/v1/admin/trust/policies/${id}/save`,'POST',value)
export const cloneTrustPolicy=(id:string,value:object)=>send<TrustPolicy>(`/api/v1/admin/trust/policies/${id}/clone`,'POST',value)
export const approveTrustPolicy=(id:string,value:object)=>send<TrustPolicy>(`/api/v1/admin/trust/policies/${id}/approve`,'POST',value)
export const activateTrustPolicy=(id:string,value:object)=>send<TrustPolicy>(`/api/v1/admin/trust/policies/${id}/activate`,'POST',value)
export const retireTrustPolicy=(id:string,value:object)=>send<TrustPolicy>(`/api/v1/admin/trust/policies/${id}/retire`,'POST',value)
export const simulateTrust=(providerId:string,policyId:string)=>send<TrustCalculation>(`/api/v1/admin/trust/${providerId}/simulation`,'POST',{policyId,idempotencyKey:crypto.randomUUID()})
export const recalculateTrust=(providerId:string)=>send<TrustCalculation>(`/api/v1/admin/trust/${providerId}/recalculate`,'POST',{idempotencyKey:crypto.randomUUID()})
export const adjustTrustScore=(providerId:string,score:number,reason:string)=>send<TrustManualAdjustment>(`/api/v1/admin/trust/${providerId}/adjustment`,'POST',{score,reason,idempotencyKey:crypto.randomUUID()})
export const getTrustReferenceData=()=>request<TrustReferenceData>('/api/v1/admin/trust/reference-data')
export const initializeTrustReferenceData=()=>send<TrustReferenceData>('/api/v1/admin/trust/reference-data/initialize','POST',{})
export const createTrustRatingItem=(value:object)=>send<TrustReferenceData>('/api/v1/admin/trust/reference-data/rating-items','POST',value)
export const updateTrustRatingItem=(id:string,value:object)=>send<TrustReferenceData>(`/api/v1/admin/trust/reference-data/rating-items/${id}`,'PUT',value)
