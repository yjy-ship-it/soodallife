import type { AdminTrustDetail,AdminTrustList } from './trustManagementTypes'

interface ApiError { message?:string }
async function request<T>(path:string):Promise<T>{const response=await fetch(path,{credentials:'include'});if(!response.ok){let message='신뢰도 정보를 불러오지 못했습니다.';try{const body=await response.json() as ApiError;if(body.message)message=body.message}catch{/* 공통 안내 */}throw new Error(message)}return response.json() as Promise<T>}
function query(values:Record<string,string|number|boolean>){const result=new URLSearchParams();Object.entries(values).forEach(([key,value])=>{if(value!=='')result.set(key,String(value))});return result.toString()}
export const searchAdminTrust=(values:Record<string,string|number|boolean>)=>request<AdminTrustList>(`/api/v1/admin/trust?${query(values)}`)
export const getAdminTrust=(providerId:string)=>request<AdminTrustDetail>(`/api/v1/admin/trust/${providerId}`)
