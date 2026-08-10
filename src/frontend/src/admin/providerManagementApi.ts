import type { AdminProviderDetail, AdminProviderList } from './providerManagementTypes'
interface ApiError { message?: string }
async function request<T>(path: string): Promise<T> { const response = await fetch(path, { credentials: 'include' }); if (!response.ok) { let message = '공급자정보를 불러오지 못했습니다.'; try { const error = await response.json() as ApiError; if (error.message) message = error.message } catch { /* 공통 안내 */ } throw new Error(message) } return response.json() as Promise<T> }
export function searchAdminProviders(filters: Record<string, string | number>) { const query = new URLSearchParams(); Object.entries(filters).forEach(([key, value]) => { if (value !== '') query.set(key, String(value)) }); return request<AdminProviderList>(`/api/v1/admin/providers?${query}`) }
export const getAdminProvider = (id: string) => request<AdminProviderDetail>(`/api/v1/admin/providers/${id}`)

