import type {
  AdminCategoryOption,
  AdminCategorySummary,
  AdminServiceCategoryDetail,
  AdminServiceCategoryList,
  UpdateAdminServiceCategoryInput,
} from './serviceCategoryTypes'

interface ApiError {
  message?: string
}

async function adminRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    ...init,
    headers: init?.body ? { 'Content-Type': 'application/json', ...init.headers } : init?.headers,
  })
  if (!response.ok) {
    let message = '요청을 처리하지 못했습니다. 잠시 후 다시 시도해 주세요.'
    try {
      const error = await response.json() as ApiError
      if (error.message) message = error.message
    } catch {
      // 응답 본문이 없으면 공통 한국어 안내를 사용합니다.
    }
    throw new Error(message)
  }
  return response.json() as Promise<T>
}

export const getCategorySummary = () => adminRequest<AdminCategorySummary>('/api/v1/admin/service-categories/summary')

export const getMajorCategories = () => adminRequest<AdminCategoryOption[]>('/api/v1/admin/service-categories/majors')

export const getMiddleCategories = (majorId: string) =>
  adminRequest<AdminCategoryOption[]>(`/api/v1/admin/service-categories/majors/${majorId}/middles`)

export function searchServiceCategories(filters: {
  search: string
  majorId: string
  middleId: string
  status: string
  feeAmount: string
  feeStatus: string
  feeEffectiveFrom: string
  feeEffectiveTo: string
  page?: number
  pageSize?: number
}) {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.majorId) query.set('majorId', filters.majorId)
  if (filters.middleId) query.set('middleId', filters.middleId)
  if (filters.status) query.set('status', filters.status)
  if (filters.feeAmount) query.set('feeAmount', filters.feeAmount)
  if (filters.feeStatus) query.set('feeStatus', filters.feeStatus)
  if (filters.feeEffectiveFrom) query.set('feeEffectiveFrom', filters.feeEffectiveFrom)
  if (filters.feeEffectiveTo) query.set('feeEffectiveTo', filters.feeEffectiveTo)
  if (filters.page) query.set('page', String(filters.page))
  if (filters.pageSize) query.set('pageSize', String(filters.pageSize))
  return adminRequest<AdminServiceCategoryList>(`/api/v1/admin/service-categories/services?${query}`)
}

export const getServiceCategory = (serviceId: string) =>
  adminRequest<AdminServiceCategoryDetail>(`/api/v1/admin/service-categories/services/${serviceId}`)

export const updateServiceCategory = (serviceId: string, input: UpdateAdminServiceCategoryInput) =>
  adminRequest<AdminServiceCategoryDetail>(`/api/v1/admin/service-categories/services/${serviceId}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })
