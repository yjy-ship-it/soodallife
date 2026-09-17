import type { AdminProviderDetail, AdminProviderList } from './providerManagementTypes'
interface ApiError { message?: string }
async function request<T>(path: string): Promise<T> { const response = await fetch(path, { credentials: 'include' }); if (!response.ok) { let message = '전문가정보를 불러오지 못했습니다.'; try { const error = await response.json() as ApiError; if (error.message) message = error.message } catch { /* 공통 안내 */ } throw new Error(message) } return response.json() as Promise<T> }
export function searchAdminProviders(filters: Record<string, string | number>) { const query = new URLSearchParams(); Object.entries(filters).forEach(([key, value]) => { if (value !== '') query.set(key, String(value)) }); return request<AdminProviderList>(`/api/v1/admin/providers?${query}`) }
export const getAdminProvider = (id: string) => request<AdminProviderDetail>(`/api/v1/admin/providers/${id}`)
export async function updateAdminProvider(id:string,input:unknown) { const response=await fetch(`/api/v1/admin/providers/${id}`,{method:'PUT',credentials:'include',headers:{'Content-Type':'application/json'},body:JSON.stringify(input)});if(!response.ok){let message='전문가 정보를 저장하지 못했습니다.';try{const error=await response.json() as ApiError;if(error.message)message=error.message}catch{/* common */}throw new Error(message)}return response.json() as Promise<AdminProviderDetail> }
async function replace<T>(path:string,input:unknown,message:string){const response=await fetch(path,{method:'PUT',credentials:'include',headers:{'Content-Type':'application/json'},body:JSON.stringify(input)});if(!response.ok){try{const error=await response.json() as ApiError;throw new Error(error.message||message)}catch(error){if(error instanceof Error)throw error;throw new Error(message)}}return response.json() as Promise<T>}
export const replaceAdminProviderServices=(id:string,categoryIds:string[])=>replace<AdminProviderDetail>(`/api/v1/admin/providers/${id}/service-categories`,{categoryIds},'서비스를 저장하지 못했습니다.')
export const replaceAdminProviderAreas=(id:string,services:Array<{serviceCategoryId:string;administrativeAreaIds:string[]}>)=>replace<AdminProviderDetail>(`/api/v1/admin/providers/${id}/service-areas`,{services},'활동지역을 저장하지 못했습니다.')
export async function decideProviderService(providerId: string, serviceId: string, input: { actionCode: 'APPROVE' | 'REJECT'; decisionReason: string | null; rowVersion: string }) {
  const response = await fetch(`/api/v1/admin/providers/${providerId}/service-approvals/${serviceId}/decisions`, { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })
  if (!response.ok) { let message = '서비스 심사결정을 저장하지 못했습니다.'; try { const error = await response.json() as ApiError; if (error.message) message = error.message } catch { /* 공통 안내 */ } throw new Error(message) }
}
export async function decideAllProviderServices(providerId: string, input: { actionCode: 'APPROVE' | 'REJECT'; decisionReason: string | null; services: Array<{ serviceId: string; rowVersion: string }> }) {
  const response = await fetch(`/api/v1/admin/providers/${providerId}/service-approvals/bulk-decisions`, { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })
  if (!response.ok) { let message = '전체 서비스 심사결정을 저장하지 못했습니다.'; try { const error = await response.json() as ApiError; if (error.message) message = error.message } catch { /* 공통 안내 */ } throw new Error(message) }
}
export async function decideProviderApproval(providerId:string,input:{actionCode:'APPROVE'|'REJECT';reason:string|null;rowVersion:string}) {
  const response=await fetch(`/api/v1/admin/providers/${providerId}/approval-decisions`,{method:'POST',credentials:'include',headers:{'Content-Type':'application/json'},body:JSON.stringify(input)})
  if(!response.ok){let message='전문가 전체 심사결정을 저장하지 못했습니다.';try{const error=await response.json() as ApiError;if(error.message)message=error.message}catch{/* 공통 안내 */}throw new Error(message)}
}
