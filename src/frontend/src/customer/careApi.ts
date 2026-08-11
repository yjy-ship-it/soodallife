import type { CareApplication, CareContract, CareHome, CareProduct, CareRequest, CareVisit, CareVisitDetail, PaymentHistory, PaymentMethod, ScheduleChange, SubscriptionService } from './careTypes'

export class CareApiError extends Error {
  readonly status: number
  readonly code?: string

  constructor(message: string, status: number, code?: string) {
    super(message)
    this.status = status
    this.code = code
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(path, { credentials: 'include', ...options, headers: options?.body ? { 'Content-Type': 'application/json', ...options.headers } : options?.headers })
  if (!response.ok) {
    let message = '구독 요청을 처리하지 못했습니다.'; let code: string | undefined
    try { const value = await response.json() as { message?: string; code?: string; businessCode?: string }; message = value.message ?? message; code = value.code ?? value.businessCode } catch { /* safe fallback */ }
    throw new CareApiError(message, response.status, code)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

const post = (body: unknown): RequestInit => ({ method: 'POST', body: JSON.stringify(body) })
export const careApi = {
  services: () => request<SubscriptionService[]>('/api/v1/public/care/services'),
  products: (serviceId?: string) => request<CareProduct[]>(`/api/v1/public/care/products${serviceId ? `?serviceId=${serviceId}` : ''}`),
  home: () => request<CareHome>('/api/v1/customers/me/care/home'),
  createRequest: (body: unknown) => request<CareRequest>('/api/v1/subscriptions/requests', post(body)),
  requests: () => request<CareRequest[]>('/api/v1/customers/me/care/requests'),
  request: (id: string) => request<CareRequest>(`/api/v1/customers/me/care/requests/${id}`),
  applications: (id: string) => request<CareApplication[]>(`/api/v1/customers/me/care/requests/${id}/applications`),
  select: (requestId: string, applicationId: string) => request<CareContract>(`/api/v1/subscriptions/requests/${requestId}/selection`, post({ applicationId, idempotencyKey: `customer-care-selection-${requestId}-${crypto.randomUUID()}` })),
  contracts: (status?: string) => request<CareContract[]>(`/api/v1/customers/me/care/contracts${status ? `?status=${status}` : ''}`),
  contract: (id: string) => request<CareContract>(`/api/v1/customers/me/care/contracts/${id}`),
  contractAction: (id: string, action: 'pause' | 'resume' | 'terminate', body: unknown) => request<CareContract>(`/api/v1/customers/me/care/contracts/${id}/${action}`, post(body)),
  visits: (contractId?: string, status?: string) => request<CareVisit[]>(`/api/v1/customers/me/care/visits?${new URLSearchParams({ ...(contractId ? { contractId } : {}), ...(status ? { status } : {}) })}`),
  visit: (id: string) => request<CareVisitDetail>(`/api/v1/customers/me/care/visits/${id}`),
  scheduleChange: (id: string, body: unknown) => request<ScheduleChange>(`/api/v1/subscriptions/visits/${id}/schedule-changes`, post(body)),
  cancelScheduleChange: (id: string, rowVersion: string) => request<ScheduleChange>(`/api/v1/customers/me/care/schedule-changes/${id}/cancel`, post({ idempotencyKey: `customer-care-change-cancel-${id}-${crypto.randomUUID()}`, rowVersion })),
  skip: (id: string, rowVersion: string, reason: string) => request<CareVisit>(`/api/v1/customers/me/care/visits/${id}/skip`, post({ idempotencyKey: `customer-care-skip-${id}-${crypto.randomUUID()}`, rowVersion, reason })),
  confirm: (id: string, rowVersion: string) => request<CareVisit>(`/api/v1/subscriptions/visits/${id}/confirmation`, post({ idempotencyKey: `customer-care-confirm-${id}-${crypto.randomUUID()}`, rowVersion })),
  review: (id: string, bodyText: string, overallRating: number) => request<string>(`/api/v1/subscriptions/visits/${id}/review`, post({ bodyText, overallRating, idempotencyKey: `customer-care-review-${id}-${crypto.randomUUID()}` })),
  afterService: (id: string, subject: string, description: string) => request<string>(`/api/v1/subscriptions/visits/${id}/after-service`, post({ subject, description, idempotencyKey: `customer-care-as-${id}-${crypto.randomUUID()}` })),
  dispute: (id: string, subject: string, description: string) => request<string>(`/api/v1/subscriptions/visits/${id}/dispute`, post({ subject, description, idempotencyKey: `customer-care-dispute-${id}-${crypto.randomUUID()}` })),
  paymentMethods: () => request<PaymentMethod[]>('/api/v1/customers/me/care/payment-methods'),
  payments: () => request<PaymentHistory[]>('/api/v1/customers/me/care/payments'),
}
