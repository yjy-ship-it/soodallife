import type { AdminRequestDetail, AdminRequestList, AdminTransactionDetail, AdminTransactionList } from './operationTypes'

async function get<T>(path:string):Promise<T>{const response=await fetch(path,{credentials:'include'});if(!response.ok){let message='운영정보를 불러오지 못했습니다.';try{const body=await response.json() as {message?:string};if(body.message)message=body.message}catch{/* 공통 안내 */}throw new Error(message)}return response.json() as Promise<T>}
function query(values:Record<string,string|number>){const result=new URLSearchParams();Object.entries(values).forEach(([key,value])=>{if(value!==''&&value!==undefined)result.set(key,String(value))});return result.toString()}
export const searchAdminRequests=(values:Record<string,string|number>)=>get<AdminRequestList>(`/api/v1/admin/requests?${query(values)}`)
export const getAdminRequest=(id:string)=>get<AdminRequestDetail>(`/api/v1/admin/requests/${id}`)
export const searchAdminTransactions=(values:Record<string,string|number>)=>get<AdminTransactionList>(`/api/v1/admin/transactions?${query(values)}`)
export const getAdminTransaction=(id:string)=>get<AdminTransactionDetail>(`/api/v1/admin/transactions/${id}`)
