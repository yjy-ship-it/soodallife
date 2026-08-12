import type { ApiErrorBody } from '../requests/types'
import type { MatchedRequestDetail, MatchedRequestListItem, ProviderAfterServiceDetail, ProviderAfterServiceListItem, ProviderCaseFile, ProviderDashboard, ProviderDisputeDetail, ProviderDisputeListItem, ProviderDocument, ProviderDocumentType, ProviderLegalDocument, ProviderOperationsDashboard, ProviderProfile, ProviderRequirement, ProviderServiceArea, ProviderServiceCategory } from './types'

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
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

const request = <T,>(path: string, init?: RequestInit) => fetch(path, { credentials: 'include', ...init }).then(readJson<T>)

export const getProviderProfile = () => request<ProviderProfile>('/api/v1/providers/me')
export const updateProviderProfile = (value: Partial<ProviderProfile>) => request<ProviderProfile>('/api/v1/providers/me', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(value) })
export const getProviderDashboard = () => request<ProviderDashboard>('/api/v1/providers/me/onboarding-dashboard')
export const getProviderOperationsDashboard = () => request<ProviderOperationsDashboard>('/api/v1/providers/me/operations-dashboard')
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
export const getProviderRequirements = () => request<ProviderRequirement[]>('/api/v1/providers/me/requirements')
export const getProviderDocumentTypes = () => request<ProviderDocumentType[]>('/api/v1/providers/me/document-types')
export const getProviderDocuments = () => request<ProviderDocument[]>('/api/v1/providers/me/documents')
export const uploadProviderDocument = (typeId: string, file: File, documentNumber?: string, issuedAt?: string, expiresAt?: string) => { const body = new FormData(); body.append('documentTypeId', typeId); if (documentNumber) body.append('documentNumber', documentNumber); if (issuedAt) body.append('issuedAt', issuedAt); if (expiresAt) body.append('expiresAt', expiresAt); body.append('file', file); return request<ProviderDocument>('/api/v1/providers/me/documents', { method: 'POST', body }) }
export const linkProviderEvidence = (verificationId: string, documentId: string) => request<ProviderRequirement>(`/api/v1/providers/me/requirements/${verificationId}/evidence`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ documentId }) })
export const resubmitProviderService = (categoryId: string) => request<void>(`/api/v1/providers/me/service-categories/${categoryId}/resubmit`, { method: 'POST' })
export const getProviderLegalDocuments = () => request<ProviderLegalDocument[]>('/api/v1/public/provider-registration/legal-documents')
export const registerProvider = (value: unknown) => request<{ userId: string; providerId: string; loginId: string; roles: string[] }>('/api/v1/public/provider-registration', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(value) })
export const addProviderRole = (value: unknown) => request<{ userId: string; providerId: string; loginId: string; roles: string[] }>('/api/v1/provider-registration/role', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(value) })
export const getProviderAfterServices = () => request<ProviderAfterServiceListItem[]>('/api/v1/providers/me/after-services')
export const getProviderAfterService = (id:string) => request<ProviderAfterServiceDetail>(`/api/v1/providers/me/after-services/${id}`)
const command = <T,>(path:string, value:unknown) => request<T>(path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(value)})
export const confirmProviderAfterService = (id:string,value:unknown) => command<ProviderAfterServiceDetail>(`/api/v1/providers/me/after-services/${id}/confirm`,value)
export const scheduleProviderAfterService = (id:string,value:unknown) => command<ProviderAfterServiceDetail>(`/api/v1/providers/me/after-services/${id}/visit-schedule`,value)
export const addProviderAfterServiceAction = (id:string,value:unknown) => command<ProviderAfterServiceDetail>(`/api/v1/providers/me/after-services/${id}/actions`,value)
export const completeProviderAfterService = (id:string,value:unknown) => command<ProviderAfterServiceDetail>(`/api/v1/providers/me/after-services/${id}/completion-report`,value)
export const uploadProviderAfterServiceEvidence = (id:string,file:File,role:string,description:string) => {const body=new FormData();body.append('file',file);body.append('role',role);body.append('description',description);return request<ProviderCaseFile>(`/api/v1/providers/me/after-services/${id}/evidence`,{method:'POST',body})}
export const getProviderDisputes = () => request<ProviderDisputeListItem[]>('/api/v1/providers/me/disputes')
export const getProviderDispute = (id:string) => request<ProviderDisputeDetail>(`/api/v1/providers/me/disputes/${id}`)
export const respondProviderDispute = (id:string,value:unknown) => command<ProviderDisputeDetail>(`/api/v1/providers/me/disputes/${id}/responses`,value)
export const uploadProviderDisputeEvidence = (id:string,file:File,description:string) => {const body=new FormData();body.append('file',file);body.append('description',description);return request<ProviderCaseFile>(`/api/v1/providers/me/disputes/${id}/evidence`,{method:'POST',body})}
