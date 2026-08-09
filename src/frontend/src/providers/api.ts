import type { ApiErrorBody } from '../requests/types'
import type { MatchedRequestDetail, MatchedRequestListItem, ProviderProfile, ProviderServiceArea, ProviderServiceCategory } from './types'

export class ProviderApiError extends Error {
  readonly status: number
  constructor(message: string, status: number) { super(message); this.status = status }
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let body: ApiErrorBody | null = null
    try { body = (await response.json()) as ApiErrorBody } catch { /* generic response below */ }
    throw new ProviderApiError(body?.message ?? '요청을 처리하지 못했습니다.', response.status)
  }
  return (await response.json()) as T
}

const request = <T,>(path: string, init?: RequestInit) => fetch(path, { credentials: 'include', ...init }).then(readJson<T>)

export const getProviderProfile = () => request<ProviderProfile>('/api/v1/providers/me')
export const getProviderServices = () => request<ProviderServiceCategory[]>('/api/v1/providers/me/service-categories')
export const replaceProviderServices = (categoryIds: string[]) => request<ProviderServiceCategory[]>('/api/v1/providers/me/service-categories', {
  method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ categoryIds }),
})
export const getProviderAreas = () => request<ProviderServiceArea[]>('/api/v1/providers/me/service-areas')
export const replaceProviderAreas = (services: Array<{ serviceCategoryId: string; administrativeAreaIds: string[] }>) => request<ProviderServiceArea[]>('/api/v1/providers/me/service-areas', {
  method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ services }),
})
export const getMatchedRequests = () => request<MatchedRequestListItem[]>('/api/v1/providers/me/matched-requests')
export const getMatchedRequest = (id: string) => request<MatchedRequestDetail>(`/api/v1/providers/me/matched-requests/${id}`)
