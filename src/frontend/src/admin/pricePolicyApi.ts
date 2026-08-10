import type { AdminPricePolicy, AdminPricePolicyList, SaveAdminPricePolicyInput, SaveAdminPricePolicyOptionInput, SaveAdminPricePolicySurchargeInput } from './pricePolicyTypes'

interface ApiError { message?: string }

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include', ...init,
    headers: init?.body ? { 'Content-Type': 'application/json', ...init.headers } : init?.headers,
  })
  if (!response.ok) {
    let message = '가격정책 요청을 처리하지 못했습니다.'
    try { const error = await response.json() as ApiError; if (error.message) message = error.message } catch { /* 공통 안내 사용 */ }
    throw new Error(message)
  }
  return response.json() as Promise<T>
}

const basePath = (serviceId: string) => `/api/v1/admin/service-categories/services/${serviceId}/price-policies`
export const getAdminPricePolicies = (serviceId: string) => request<AdminPricePolicyList>(basePath(serviceId))
export const getCurrentAdminPricePolicy = (serviceId: string) => request<AdminPricePolicy>(`${basePath(serviceId)}/current`)
export const createAdminPricePolicy = (serviceId: string, input: SaveAdminPricePolicyInput) =>
  request<AdminPricePolicy>(basePath(serviceId), { method: 'POST', body: JSON.stringify(input) })
export const updateAdminPricePolicy = (serviceId: string, policyId: string, input: SaveAdminPricePolicyInput) =>
  request<AdminPricePolicy>(`${basePath(serviceId)}/${policyId}`, { method: 'PUT', body: JSON.stringify(input) })
export const createAdminPricePolicyOption = (serviceId: string, policyId: string, input: SaveAdminPricePolicyOptionInput) =>
  request<AdminPricePolicy>(`${basePath(serviceId)}/${policyId}/options`, { method: 'POST', body: JSON.stringify(input) })
export const updateAdminPricePolicyOption = (serviceId: string, policyId: string, optionId: string, input: SaveAdminPricePolicyOptionInput) =>
  request<AdminPricePolicy>(`${basePath(serviceId)}/${policyId}/options/${optionId}`, { method: 'PUT', body: JSON.stringify(input) })
export const createAdminPricePolicySurcharge = (serviceId: string, policyId: string, input: SaveAdminPricePolicySurchargeInput) =>
  request<AdminPricePolicy>(`${basePath(serviceId)}/${policyId}/surcharges`, { method: 'POST', body: JSON.stringify(input) })
export const updateAdminPricePolicySurcharge = (serviceId: string, policyId: string, surchargeId: string, input: SaveAdminPricePolicySurchargeInput) =>
  request<AdminPricePolicy>(`${basePath(serviceId)}/${policyId}/surcharges/${surchargeId}`, { method: 'PUT', body: JSON.stringify(input) })
