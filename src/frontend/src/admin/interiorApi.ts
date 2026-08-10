import type {InteriorProjectDetail,InteriorProjectItem} from './interiorTypes'
async function request<T>(path:string):Promise<T>{const response=await fetch(path,{credentials:'include'});if(!response.ok){const body=await response.json().catch(()=>null) as {message?:string}|null;throw new Error(body?.message??'인테리어 운영정보를 불러오지 못했습니다.')}return response.json() as Promise<T>}
export const getInteriorProjects=(params:URLSearchParams)=>request<InteriorProjectItem[]>(`/api/v1/admin/interior/projects?${params}`)
export const getInteriorProject=(id:string)=>request<InteriorProjectDetail>(`/api/v1/admin/interior/projects/${id}`)
