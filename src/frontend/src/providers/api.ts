import type { ApiErrorBody } from '../requests/types'
import type { MatchedRequestDetail, MatchedRequestListItem, ProviderAfterServiceDetail, ProviderAfterServiceListItem, ProviderCaseFile, ProviderDashboard, ProviderDisputeDetail, ProviderDisputeListItem, ProviderDocument, ProviderDocumentType, ProviderLegalDocument, ProviderOperationsDashboard, ProviderOperationsHub, ProviderProfile, ProviderRequirement, ProviderServiceArea, ProviderServiceCategory } from './types'

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

const request = <T,>(path: string, init?: RequestInit) => fetch(path, { credentials: 'include', ...init }).then(readJson<T>).catch(error => {
  if (error instanceof ProviderApiError) throw error
  if (error instanceof TypeError) throw new ProviderApiError('서버에 연결하지 못했습니다. 잠시 후 다시 시도해 주세요.', 0)
  throw error
})

export const getProviderProfile = () => request<ProviderProfile>('/api/v1/providers/me')
export const updateProviderProfile = (value: Partial<ProviderProfile>) => request<ProviderProfile>('/api/v1/providers/me', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(value) })
const imageChunkSize = 64 * 1024
const imageUploadIds = new WeakMap<File,string>()
const imageUploadId = (file:File) => { const existing=imageUploadIds.get(file); if(existing)return existing; const created=crypto.randomUUID(); imageUploadIds.set(file,created); return created }
const wait = (milliseconds:number) => new Promise(resolve=>window.setTimeout(resolve,milliseconds))
const encodeImageChunk = (bytes:Uint8Array) => { let binary=''; for(let offset=0;offset<bytes.length;offset+=32768)binary+=String.fromCharCode(...bytes.subarray(offset,offset+32768)); return btoa(binary) }
const uploadProviderPromotionImage = async (file:File, purpose:'LOGO'|'PHOTO', replaceExisting:boolean) => {
  const bytes=new Uint8Array(await file.arrayBuffer())
  const totalChunks=Math.ceil(bytes.length/imageChunkSize)
  const uploadId=imageUploadId(file)
  let profile:ProviderProfile|null=null
  for(let chunkIndex=0;chunkIndex<totalChunks;chunkIndex++) {
    const chunk=bytes.subarray(chunkIndex*imageChunkSize,Math.min(bytes.length,(chunkIndex+1)*imageChunkSize))
    const body=JSON.stringify({uploadId,purpose,replaceExisting,fileName:file.name,contentType:file.type,chunkIndex,totalChunks,base64Chunk:encodeImageChunk(chunk)})
    let result:{completed:boolean;profile:ProviderProfile|null}|null=null
    for(let attempt=0;attempt<3;attempt++) {
      try { result=await request<{completed:boolean;profile:ProviderProfile|null}>('/api/v1/providers/me/promotion-images/chunks',{method:'POST',headers:{'Content-Type':'application/json'},body}); break }
      catch(error) { if(!(error instanceof ProviderApiError)||error.status!==0||attempt===2)throw error; await wait(400*(attempt+1)) }
    }
    if(!result) throw new ProviderApiError('이미지 조각을 전송하지 못했습니다. 다시 시도해 주세요.',0)
    if(result.completed) profile=result.profile
  }
  if(!profile) throw new ProviderApiError('이미지 업로드를 완료하지 못했습니다. 다시 시도해 주세요.',0)
  return profile
}
export const uploadProviderPromotionLogo = (file: File) => uploadProviderPromotionImage(file,'LOGO',true)
export const uploadProviderPromotionPhoto = (file: File, replaceExisting: boolean) => uploadProviderPromotionImage(file,'PHOTO',replaceExisting)
export const getProviderPromotionStorageStatus = () => request<{ writable:boolean; message:string }>('/api/v1/providers/me/promotion-images/storage-status')
export const getProviderDashboard = () => request<ProviderDashboard>('/api/v1/providers/me/onboarding-dashboard')
export const getProviderOperationsDashboard = () => request<ProviderOperationsDashboard>('/api/v1/providers/me/operations-dashboard')
export const getProviderOperationsHub = (query: { group?:string; domain?:string; page?:number; pageSize?:number } = {}) => {
  const params = new URLSearchParams()
  if (query.group) params.set('group', query.group)
  if (query.domain) params.set('domain', query.domain)
  if (query.page) params.set('page', String(query.page))
  if (query.pageSize) params.set('pageSize', String(query.pageSize))
  return request<ProviderOperationsHub>(`/api/v1/providers/me/operations-hub${params.size ? `?${params}` : ''}`)
}
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
export const getIdentityVerificationStatus = () => request<{ statusCode: string; isVerified: boolean }>('/api/v1/public/customer-account/identity-verification/status')
export const getPhoneAvailability = (value: string) => request<{ available: boolean; normalizedValue?: string }>(`/api/v1/public/customer-account/availability/phone?value=${encodeURIComponent(value)}`)
export const getLoginAvailability = (value: string) => request<{ available: boolean; normalizedValue?: string }>(`/api/v1/public/provider-registration/availability/login-id?value=${encodeURIComponent(value)}`)
export const getBusinessRegistrationAvailability = (value: string) => request<{ valid: boolean; available: boolean; normalizedValue: string }>(`/api/v1/public/provider-registration/availability/business-registration-number?value=${encodeURIComponent(value)}`)
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
