import type { AdminFeePolicy, AdminFeePolicyList, SaveAdminFeePolicyInput } from './feePolicyTypes'

interface ApiError { message?: string }

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    ...init,
    headers: init?.body ? { 'Content-Type': 'application/json', ...init.headers } : init?.headers,
  })
  if (!response.ok) {
    let message = '수수료정책 요청을 처리하지 못했습니다.'
    try {
      const error = await response.json() as ApiError
      if (error.message) message = error.message
    } catch { /* 공통 안내 사용 */ }
    throw new Error(message)
  }
  return response.json() as Promise<T>
}

const basePath = (serviceId: string) => `/api/v1/admin/service-categories/services/${serviceId}/fee-policies`

export const getAdminFeePolicies = (serviceId: string) => request<AdminFeePolicyList>(basePath(serviceId))
export const createAdminFeePolicy = (serviceId: string, input: SaveAdminFeePolicyInput) =>
  request<AdminFeePolicy>(basePath(serviceId), { method: 'POST', body: JSON.stringify(input) })
export const updateAdminFeePolicy = (serviceId: string, policyId: string, input: SaveAdminFeePolicyInput) =>
  request<AdminFeePolicy>(`${basePath(serviceId)}/${policyId}`, { method: 'PUT', body: JSON.stringify(input) })
