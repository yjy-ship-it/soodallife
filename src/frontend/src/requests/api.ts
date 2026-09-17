import type { AdministrativeArea, ApiErrorBody, Category, RequestField, RequestFile, ServiceRequestDetail, ServiceRequestListItem } from './types'
import { apiUrl } from '../config/apiEndpoint'

export class RequestApiError extends Error { readonly status: number; readonly fieldErrors?: Record<string, string[]>; readonly traceId?: string; constructor(message: string, status: number, fieldErrors?: Record<string, string[]>, traceId?: string) { super(message); this.status = status; this.fieldErrors = fieldErrors; this.traceId = traceId } }
export class RequestNetworkError extends TypeError {
  readonly path: string
  readonly causeMessage: string
  constructor(path: string, causeMessage: string) {
    super('REQUEST_NETWORK_ERROR')
    this.path = path
    this.causeMessage = causeMessage
  }
}
async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let body: ApiErrorBody | null = null
    try { body = await response.json() as ApiErrorBody } catch { /* safe fallback */ }
    throw new RequestApiError(body?.message ?? '요청을 처리하지 못했습니다.', response.status, body?.fieldErrors, body?.traceId)
  }
  if (response.status === 204) return undefined as T
  if (!response.headers.get('content-type')?.toLowerCase().includes('application/json')) {
    throw new RequestApiError('API 서버의 응답을 확인하지 못했습니다. 새로고침 후 다시 시도해 주세요.', 502)
  }
  return response.json() as Promise<T>
}

const pause = (milliseconds: number) => new Promise<void>((resolve) => window.setTimeout(resolve, milliseconds))

async function request<T>(path: string, init: RequestInit, networkRetryCount = 0): Promise<T> {
  let attempt = 0
  while (true) {
    try {
      const response = await fetch(apiUrl(path), { cache: 'no-store', credentials: 'include', ...init })
      return await readJson<T>(response)
    } catch (reason) {
      if (!(reason instanceof TypeError)) throw reason
      if (attempt >= networkRetryCount) {
        throw new RequestNetworkError(path, reason.message)
      }
      attempt += 1
      await pause(500)
    }
  }
}

const get = <T,>(path: string) => request<T>(path, { method: 'GET' })
const send = <T,>(method: string, path: string, body?: unknown, networkRetryCount = 0) => request<T>(path, {
  method,
  headers: body === undefined ? undefined : { 'Content-Type': 'application/json', Accept: 'application/json' },
  body: body === undefined ? undefined : JSON.stringify(body),
}, networkRetryCount)
const categoryQuery = (emergencyOnly: boolean, includeSubscription: boolean) => {
  const params = new URLSearchParams()
  if (emergencyOnly) params.set('emergencyOnly', 'true')
  if (includeSubscription) params.set('includeSubscription', 'true')
  const query = params.toString()
  return query ? `?${query}` : ''
}
export const getMajorCategories = (emergencyOnly = false, includeSubscription = false) => get<Category[]>(`/api/v1/categories/majors${categoryQuery(emergencyOnly, includeSubscription)}`)
export const getMiddleCategories = (id: string, emergencyOnly = false, includeSubscription = false) => get<Category[]>(`/api/v1/categories/${id}/middles${categoryQuery(emergencyOnly, includeSubscription)}`)
export const getServiceCategories = (id: string, emergencyOnly = false, includeSubscription = false) => get<Category[]>(`/api/v1/categories/${id}/services${categoryQuery(emergencyOnly, includeSubscription)}`)
export const getRequestFields = (id: string) => get<RequestField[]>(`/api/v1/categories/${id}/request-fields`)
export const getAdministrativeAreas = () => get<AdministrativeArea[]>('/api/v1/administrative-areas/sigungu')
export const getSidoAreas = () => get<AdministrativeArea[]>('/api/v1/administrative-areas/sidos')
export const getMyRequests = () => get<ServiceRequestListItem[]>('/api/v1/requests')
export const getMyRequest = (id: string) => get<ServiceRequestDetail>(`/api/v1/requests/${id}`)
export const createServiceRequest = (payload: unknown) => send<{ id: string; status: string }>('POST', '/api/v1/requests', payload, 1)
export const updateServiceRequest = (id: string, payload: unknown) => send<ServiceRequestDetail>('POST', `/api/v1/requests/${id}/draft`, payload, 1)
export const publishServiceRequest = (id: string) => send<{ id: string; status: string; eligibleCandidateCount: number; dispatchCount: number; customerMessage: string }>('POST', `/api/v1/requests/${id}/publish`)
export const cancelServiceRequest = (id: string, reason: string) => send<ServiceRequestDetail>('POST', `/api/v1/requests/${id}/cancel`, { reason })
export async function uploadRequestFile(id: string, file: File, requestFieldId?: string): Promise<RequestFile> { const form = new FormData(); form.append('file', file); if (requestFieldId) form.append('requestFieldId', requestFieldId); return request(`/api/v1/requests/${id}/files`, { method: 'POST', body: form }) }
export const deleteRequestFile = (requestId: string, fileId: string) => send<void>('DELETE', `/api/v1/requests/${requestId}/files/${fileId}`)
