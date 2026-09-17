import type { AdminProviderDocumentType, AdminProviderRequirementDefaults, AdminProviderRequirementDefinition, AdminProviderRequirementStandards, AdminProviderRequirementType, SaveProviderDocumentTypeInput, SaveProviderRequirementDefinitionInput, SaveProviderRequirementTypeInput } from './providerRequirementStandardTypes'

interface ApiError { message?: string }
async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { credentials: 'include', ...init, headers: init?.body ? { 'Content-Type': 'application/json', ...init.headers } : init?.headers })
  if (!response.ok) { let message = '전문가 요건 기준정보 요청을 처리하지 못했습니다.'; try { const error = await response.json() as ApiError; if (error.message) message = error.message } catch { /* 공통 안내 사용 */ } throw new Error(message) }
  return response.json() as Promise<T>
}
const base = '/api/v1/admin/provider-requirement-standards'
export const getProviderRequirementStandards = () => request<AdminProviderRequirementStandards>(base)
export const createProviderRequirementType = (input: SaveProviderRequirementTypeInput) => request<AdminProviderRequirementType>(`${base}/types`, { method: 'POST', body: JSON.stringify(input) })
export const updateProviderRequirementType = (code: string, input: SaveProviderRequirementTypeInput) => request<AdminProviderRequirementType>(`${base}/types/${code}`, { method: 'PUT', body: JSON.stringify(input) })
export const createProviderRequirementDefinition = (input: SaveProviderRequirementDefinitionInput) => request<AdminProviderRequirementDefinition>(`${base}/definitions`, { method: 'POST', body: JSON.stringify(input) })
export const updateProviderRequirementDefinition = (id: string, input: SaveProviderRequirementDefinitionInput) => request<AdminProviderRequirementDefinition>(`${base}/definitions/${id}`, { method: 'PUT', body: JSON.stringify(input) })
export const createProviderDocumentType = (input: SaveProviderDocumentTypeInput) => request<AdminProviderDocumentType>(`${base}/document-types`, { method: 'POST', body: JSON.stringify(input) })
export const updateProviderDocumentType = (id: string, input: SaveProviderDocumentTypeInput) => request<AdminProviderDocumentType>(`${base}/document-types/${id}`, { method: 'PUT', body: JSON.stringify(input) })
export const applyProviderRequirementDefaults = () => request<AdminProviderRequirementDefaults>(`${base}/defaults/apply`, { method: 'POST' })
