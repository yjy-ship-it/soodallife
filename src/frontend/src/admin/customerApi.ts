import type { AdminCustomerDetail, AdminCustomerList } from './customerTypes'

interface ApiError { message?: string }

async function request<T>(path: string): Promise<T> {
  const response = await fetch(path, { credentials: 'include' })
  if (!response.ok) {
    let message = '고객정보를 불러오지 못했습니다.'
    try { const error = await response.json() as ApiError; if (error.message) message = error.message } catch { /* 공통 안내 사용 */ }
    throw new Error(message)
  }
  return response.json() as Promise<T>
}

export function searchAdminCustomers(filters: { search: string; status: string; hasRequests: string; hasTransactions: string; joinedFrom: string; joinedTo: string; page: number; pageSize: number }) {
  const query = new URLSearchParams({ page: String(filters.page), pageSize: String(filters.pageSize) })
  if (filters.search) query.set('search', filters.search)
  if (filters.status) query.set('status', filters.status)
  if (filters.hasRequests) query.set('hasRequests', filters.hasRequests)
  if (filters.hasTransactions) query.set('hasTransactions', filters.hasTransactions)
  if (filters.joinedFrom) query.set('joinedFrom', filters.joinedFrom)
  if (filters.joinedTo) query.set('joinedTo', filters.joinedTo)
  return request<AdminCustomerList>(`/api/v1/admin/customers?${query}`)
}

export const getAdminCustomer = (customerId: string) => request<AdminCustomerDetail>(`/api/v1/admin/customers/${customerId}`)

export async function setAdminCustomerRequestAbuseExclusion(customerId: string, requestId: string, excluded: boolean, reason: string) {
  const response = await fetch(`/api/v1/admin/customers/${customerId}/requests/${requestId}/abuse-exclusion`, {
    method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ excluded, reason }),
  })
  if (!response.ok) {
    let message = '요청 제한 예외를 변경하지 못했습니다.'
    try { const error = await response.json() as ApiError; if (error.message) message = error.message } catch { /* 공통 안내 사용 */ }
    throw new Error(message)
  }
}
