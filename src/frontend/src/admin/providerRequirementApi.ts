import type { AdminProviderRequirement, AdminProviderRequirementList, SaveAdminCategoryProviderRequirementInput, SaveAdminOperationPolicyInput } from './providerRequirementTypes'

interface ApiError { message?: string }

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { credentials: 'include', ...init, headers: init?.body ? { 'Content-Type': 'application/json', ...init.headers } : init?.headers })
  if (!response.ok) {
    let message = '전문가 요건을 불러오지 못했습니다.'
    try {
      const error = await response.json() as ApiError
      if (error.message) message = error.message
    } catch { /* 공통 안내 사용 */ }
    throw new Error(message)
  }
  return response.json() as Promise<T>
}

const basePath = (serviceId: string) => `/api/v1/admin/service-categories/services/${serviceId}/provider-requirements`
export const getAdminProviderRequirements = (serviceId: string) => request<AdminProviderRequirementList>(basePath(serviceId))
export const createAdminProviderRequirement = (serviceId: string, policyId: string, input: SaveAdminCategoryProviderRequirementInput) =>
  request<AdminProviderRequirement>(`${basePath(serviceId)}/${policyId}/assignments`, { method: 'POST', body: JSON.stringify(input) })
export const updateAdminProviderRequirement = (serviceId: string, policyId: string, assignmentId: string, input: SaveAdminCategoryProviderRequirementInput) =>
  request<AdminProviderRequirement>(`${basePath(serviceId)}/${policyId}/assignments/${assignmentId}`, { method: 'PUT', body: JSON.stringify(input) })
export const replaceAdminProviderRequirementEvidence = (serviceId: string, policyId: string, assignmentId: string, evidenceTypes: Array<{ documentTypeId: string; isRequired: boolean; displayOrder: number }>) =>
  request<AdminProviderRequirement>(`${basePath(serviceId)}/${policyId}/assignments/${assignmentId}/evidence-types`, { method: 'PUT', body: JSON.stringify({ evidenceTypes }) })
export const updateAdminOperationPolicy = (serviceId: string, policyId: string, input: SaveAdminOperationPolicyInput) =>
  request<AdminProviderRequirement>(`${basePath(serviceId)}/${policyId}/operation-policy`, { method: 'PUT', body: JSON.stringify(input) })
export const updateAdminMiddleOperationPolicies = (middleId: string, input: Omit<SaveAdminOperationPolicyInput, 'rowVersion'> & { services: Array<{ serviceId: string; policyId: string; rowVersion: string }> }) =>
  request<AdminProviderRequirement[]>(`/api/v1/admin/service-categories/middles/${middleId}/operation-policy`, { method: 'PUT', body: JSON.stringify(input) })
export const applyAdminMiddleProviderRequirements = (middleId: string, input: { sourceServiceId: string; sourcePolicyId: string; services: Array<{ serviceId: string; policyId: string; rowVersion: string }> }) =>
  request<AdminProviderRequirement[]>(`/api/v1/admin/service-categories/middles/${middleId}/provider-requirements/apply`, { method: 'PUT', body: JSON.stringify(input) })
