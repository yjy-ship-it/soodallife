import type { AdministrativeArea, ApiErrorBody, Category, RequestField, ServiceRequestDetail, ServiceRequestListItem } from './types'

export class RequestApiError extends Error {
  readonly status: number
  readonly fieldErrors?: Record<string, string[]>

  constructor(message: string, status: number, fieldErrors?: Record<string, string[]>) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let body: ApiErrorBody | null = null
    try { body = (await response.json()) as ApiErrorBody } catch { /* safe generic response below */ }
    throw new RequestApiError(body?.message ?? '요청을 처리하지 못했습니다.', response.status, body?.fieldErrors)
  }
  return (await response.json()) as T
}

const get = <T,>(path: string) => fetch(path, { credentials: 'include' }).then(readJson<T>)

export const getMajorCategories = () => get<Category[]>('/api/v1/categories/majors')
export const getMiddleCategories = (id: string) => get<Category[]>(`/api/v1/categories/${id}/middles`)
export const getServiceCategories = (id: string) => get<Category[]>(`/api/v1/categories/${id}/services`)
export const getRequestFields = (id: string) => get<RequestField[]>(`/api/v1/categories/${id}/request-fields`)
export const getAdministrativeAreas = () => get<AdministrativeArea[]>('/api/v1/administrative-areas/sigungu')
export const getSidoAreas = () => get<AdministrativeArea[]>('/api/v1/administrative-areas/sidos')
export const getMyRequests = () => get<ServiceRequestListItem[]>('/api/v1/requests')
export const getMyRequest = (id: string) => get<ServiceRequestDetail>(`/api/v1/requests/${id}`)

export async function createServiceRequest(payload: unknown): Promise<{ id: string; status: string }> {
  return readJson(await fetch('/api/v1/requests', {
    method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload),
  }))
}

export async function publishServiceRequest(id: string): Promise<{ id: string; status: string; eligibleCandidateCount: number; dispatchCount: number }> {
  return readJson(await fetch(`/api/v1/requests/${id}/publish`, {
    method: 'POST', credentials: 'include',
  }))
}
