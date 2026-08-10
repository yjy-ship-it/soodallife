import type{AdminReviewDetail,AdminReviewList}from'./reviewManagementTypes'
async function request<T>(path:string,init?:RequestInit):Promise<T>{const response=await fetch(path,{credentials:'include',headers:init?.body?{'Content-Type':'application/json'}:undefined,...init});if(!response.ok){let message='리뷰 정보를 처리하지 못했습니다.';try{const value=await response.json() as{message?:string};if(value.message)message=value.message}catch{/* 공통 안내 */}throw new Error(message)}return response.json() as Promise<T>}
function query(values:Record<string,string|number>){const result=new URLSearchParams();Object.entries(values).forEach(([key,value])=>{if(value!=='')result.set(key,String(value))});return result.toString()}
export const searchAdminReviews=(values:Record<string,string|number>)=>request<AdminReviewList>(`/api/v1/admin/reviews?${query(values)}`)
export const getAdminReview=(id:string)=>request<AdminReviewDetail>(`/api/v1/admin/reviews/${id}`)
export const changeReviewVisibility=(id:string,value:{targetStatusCode:string;reason:string;rowVersion:string})=>request<AdminReviewDetail>(`/api/v1/admin/reviews/${id}/visibility`,{method:'POST',body:JSON.stringify(value)})
