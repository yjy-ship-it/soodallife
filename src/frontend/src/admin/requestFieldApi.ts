import type {
  AdminRequestField,
  UpdateAdminRequestFieldAssignmentInput,
  UpdateAdminRequestFieldDefinitionInput,
  UpdateAdminRequestFieldOptionInput,
} from './requestFieldTypes'

interface ApiError { message?: string }

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    ...init,
    headers: init?.body ? { 'Content-Type': 'application/json', ...init.headers } : init?.headers,
  })
  if (!response.ok) {
    let message = '요청을 처리하지 못했습니다.'
    try {
      const error = await response.json() as ApiError
      if (error.message) message = error.message
    } catch {
      // 공통 한국어 안내를 사용합니다.
    }
    throw new Error(message)
  }
  return response.json() as Promise<T>
}

const basePath = (serviceId: string) => `/api/v1/admin/service-categories/services/${serviceId}/request-fields`

export const getAdminRequestFields = (serviceId: string) => request<AdminRequestField[]>(basePath(serviceId))
export const getAdminRequestField = (serviceId: string, fieldId: string) => request<AdminRequestField>(`${basePath(serviceId)}/${fieldId}`)

export const updateAdminRequestFieldDefinition = (serviceId: string, fieldId: string, input: UpdateAdminRequestFieldDefinitionInput) =>
  request<AdminRequestField>(`${basePath(serviceId)}/${fieldId}/definition`, { method: 'PUT', body: JSON.stringify(input) })

export const updateAdminRequestFieldAssignment = (serviceId: string, fieldId: string, input: UpdateAdminRequestFieldAssignmentInput) =>
  request<AdminRequestField>(`${basePath(serviceId)}/${fieldId}/assignment`, { method: 'PUT', body: JSON.stringify(input) })

export const updateAdminRequestFieldOption = (serviceId: string, fieldId: string, optionId: string, input: UpdateAdminRequestFieldOptionInput) =>
  request<AdminRequestField>(`${basePath(serviceId)}/${fieldId}/options/${optionId}`, { method: 'PUT', body: JSON.stringify(input) })
