import type { AdministrativeArea, ApiErrorBody, Category, RequestField, RequestFile, ServiceRequestDetail, ServiceRequestListItem } from './types'

export class RequestApiError extends Error { readonly status: number; readonly fieldErrors?: Record<string, string[]>; constructor(message: string, status: number, fieldErrors?: Record<string, string[]>) { super(message); this.status = status; this.fieldErrors = fieldErrors } }
async function readJson<T>(response: Response): Promise<T> { if (!response.ok) { let body: ApiErrorBody | null = null; try { body = await response.json() as ApiErrorBody } catch { /* safe fallback */ } throw new RequestApiError(body?.message ?? '요청을 처리하지 못했습니다.', response.status, body?.fieldErrors) } return response.status === 204 ? undefined as T : response.json() as Promise<T> }
const get = <T,>(path: string) => fetch(path, { credentials: 'include' }).then(readJson<T>)
const send = <T,>(method: string, path: string, body?: unknown) => fetch(path, { method, credentials: 'include', headers: body === undefined ? undefined : { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body) }).then(readJson<T>)
const emergencyQuery = (emergencyOnly: boolean) => emergencyOnly ? '?emergencyOnly=true' : ''
export const getMajorCategories = (emergencyOnly = false) => get<Category[]>(`/api/v1/categories/majors${emergencyQuery(emergencyOnly)}`)
export const getMiddleCategories = (id: string, emergencyOnly = false) => get<Category[]>(`/api/v1/categories/${id}/middles${emergencyQuery(emergencyOnly)}`)
export const getServiceCategories = (id: string, emergencyOnly = false) => get<Category[]>(`/api/v1/categories/${id}/services${emergencyQuery(emergencyOnly)}`)
export const getRequestFields = (id: string) => get<RequestField[]>(`/api/v1/categories/${id}/request-fields`)
export const getAdministrativeAreas = () => get<AdministrativeArea[]>('/api/v1/administrative-areas/sigungu')
export const getSidoAreas = () => get<AdministrativeArea[]>('/api/v1/administrative-areas/sidos')
export const getMyRequests = () => get<ServiceRequestListItem[]>('/api/v1/requests')
export const getMyRequest = (id: string) => get<ServiceRequestDetail>(`/api/v1/requests/${id}`)
export const createServiceRequest = (payload: unknown) => send<{ id: string; status: string }>('POST', '/api/v1/requests', payload)
export const updateServiceRequest = (id: string, payload: unknown) => send<ServiceRequestDetail>('PUT', `/api/v1/requests/${id}`, payload)
export const publishServiceRequest = (id: string) => send<{ id: string; status: string; eligibleCandidateCount: number; dispatchCount: number; customerMessage: string }>('POST', `/api/v1/requests/${id}/publish`)
export const cancelServiceRequest = (id: string, reason: string) => send<ServiceRequestDetail>('POST', `/api/v1/requests/${id}/cancel`, { reason })
export async function uploadRequestFile(id: string, file: File, requestFieldId?: string): Promise<RequestFile> { const form = new FormData(); form.append('file', file); if (requestFieldId) form.append('requestFieldId', requestFieldId); return readJson(await fetch(`/api/v1/requests/${id}/files`, { method: 'POST', credentials: 'include', body: form })) }
export const deleteRequestFile = (requestId: string, fileId: string) => send<void>('DELETE', `/api/v1/requests/${requestId}/files/${fileId}`)
