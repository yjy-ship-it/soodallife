import type { AfterServiceDetail,AfterServiceList,DisputeDetail,DisputeList } from './afterServiceDisputeTypes'
async function request<T>(path:string,init?:RequestInit):Promise<T>{const response=await fetch(path,{credentials:'include',headers:init?.body?{'Content-Type':'application/json'}:undefined,...init});if(!response.ok){let message='A/S·분쟁 업무를 처리하지 못했습니다.';try{const body=await response.json() as{message?:string};if(body.message)message=body.message}catch{/* 공통 안내 */}throw new Error(message)}return response.json() as Promise<T>}
function query(values:Record<string,string|number|boolean>){const q=new URLSearchParams();Object.entries(values).forEach(([k,v])=>{if(v!=='')q.set(k,String(v))});return q.toString()}
const asRoot='/api/v1/admin/after-services',disputeRoot='/api/v1/admin/disputes'
export const searchAfterServices=(values:Record<string,string|number|boolean>)=>request<AfterServiceList>(`${asRoot}?${query(values)}`)
export const getAfterService=(id:string)=>request<AfterServiceDetail>(`${asRoot}/${id}`)
export const createAfterService=(value:unknown)=>request<AfterServiceDetail>(asRoot,{method:'POST',body:JSON.stringify(value)})
export const changeAfterServiceStatus=(id:string,value:unknown)=>request<AfterServiceDetail>(`${asRoot}/${id}/status`,{method:'POST',body:JSON.stringify(value)})
export const addAfterServiceAction=(id:string,value:unknown)=>request<AfterServiceDetail>(`${asRoot}/${id}/actions`,{method:'POST',body:JSON.stringify(value)})
export const convertAfterService=(id:string,value:unknown)=>request<DisputeDetail>(`${asRoot}/${id}/convert-to-dispute`,{method:'POST',body:JSON.stringify(value)})
export const searchDisputes=(values:Record<string,string|number|boolean>)=>request<DisputeList>(`${disputeRoot}?${query(values)}`)
export const getDispute=(id:string)=>request<DisputeDetail>(`${disputeRoot}/${id}`)
export const createDispute=(value:unknown)=>request<DisputeDetail>(disputeRoot,{method:'POST',body:JSON.stringify(value)})
export const changeDisputeStatus=(id:string,value:unknown)=>request<DisputeDetail>(`${disputeRoot}/${id}/status`,{method:'POST',body:JSON.stringify(value)})
export const assignDispute=(id:string,value:unknown)=>request<DisputeDetail>(`${disputeRoot}/${id}/assignment`,{method:'POST',body:JSON.stringify(value)})
export const resolveDispute=(id:string,value:unknown)=>request<DisputeDetail>(`${disputeRoot}/${id}/resolution`,{method:'POST',body:JSON.stringify(value)})
