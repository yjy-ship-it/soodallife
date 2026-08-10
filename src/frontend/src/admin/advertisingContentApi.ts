import type { AdvertisingCampaignDetail, AdvertisingCampaignList, AdvertisingPlacement, CampaignPayload, ContentPayload, CreativePayload, ManagedContentDetail, ManagedContentList } from './advertisingContentTypes'

async function request<T>(path:string, init?:RequestInit):Promise<T>{
  const response=await fetch(path,{credentials:'include',headers:init?.body?{'Content-Type':'application/json'}:undefined,...init})
  if(!response.ok){let message='광고·콘텐츠 정보를 처리하지 못했습니다.';try{const body=await response.json() as {message?:string};if(body.message)message=body.message}catch{/* 공통 안내 */}throw new Error(message)}
  return response.status===204 ? undefined as T : response.json() as Promise<T>
}
function query(values:Record<string,string|number>){const result=new URLSearchParams();Object.entries(values).forEach(([key,value])=>{if(value!=='')result.set(key,String(value))});return result.toString()}
const root='/api/v1/admin/advertising-content'
export const getAdvertisingPlacements=()=>request<AdvertisingPlacement[]>(`${root}/placements`)
export const searchAdvertisingCampaigns=(values:Record<string,string|number>)=>request<AdvertisingCampaignList>(`${root}/campaigns?${query(values)}`)
export const getAdvertisingCampaign=(id:string)=>request<AdvertisingCampaignDetail>(`${root}/campaigns/${id}`)
export const createAdvertisingCampaign=(value:CampaignPayload)=>request<AdvertisingCampaignDetail>(`${root}/campaigns`,{method:'POST',body:JSON.stringify(value)})
export const updateAdvertisingCampaign=(id:string,value:CampaignPayload)=>request<AdvertisingCampaignDetail>(`${root}/campaigns/${id}`,{method:'PUT',body:JSON.stringify(value)})
export const reviewAdvertisingCampaign=(id:string,actionCode:string,reason:string|null)=>request<AdvertisingCampaignDetail>(`${root}/campaigns/${id}/review`,{method:'POST',body:JSON.stringify({actionCode,reason})})
export const pauseAdvertisingCampaign=(id:string,reason:string)=>request<AdvertisingCampaignDetail>(`${root}/campaigns/${id}/pause`,{method:'POST',body:JSON.stringify({reason})})
export const createAdvertisingCreative=(id:string,value:CreativePayload)=>request(`${root}/campaigns/${id}/creatives`,{method:'POST',body:JSON.stringify(value)})
export const searchManagedContents=(values:Record<string,string|number>)=>request<ManagedContentList>(`${root}/contents?${query(values)}`)
export const getManagedContent=(id:string)=>request<ManagedContentDetail>(`${root}/contents/${id}`)
export const createManagedContent=(value:ContentPayload)=>request<ManagedContentDetail>(`${root}/contents`,{method:'POST',body:JSON.stringify(value)})
export const updateManagedContent=(id:string,value:ContentPayload)=>request<ManagedContentDetail>(`${root}/contents/${id}`,{method:'PUT',body:JSON.stringify(value)})
export const reviewManagedContent=(id:string,actionCode:string,reason:string|null)=>request<ManagedContentDetail>(`${root}/contents/${id}/review`,{method:'POST',body:JSON.stringify({actionCode,reason})})
