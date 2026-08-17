import type { AdministrativeArea, Availability, Consent, ConsentHistory, CustomerAddress, CustomerNotification, CustomerProfile, LegalDocument, MySoodalSummary, NotificationPreference, ProviderBlock, WithdrawalDashboard, WithdrawalReadiness, WithdrawalRequest } from './accountTypes'

export class CustomerAccountApiError extends Error {
  readonly status: number
  readonly businessCode?: string
  constructor(message: string, status: number, businessCode?: string) { super(message); this.status = status; this.businessCode = businessCode }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(path, { credentials: 'include', ...options, headers: options?.body ? { 'Content-Type': 'application/json', ...options.headers } : options?.headers })
  if (!response.ok) {
    let message = '요청을 처리하지 못했습니다. 잠시 후 다시 시도해 주세요.'
    let businessCode: string | undefined
    try { const value = await response.json() as { message?: string; businessCode?: string; code?: string }; message = value.message ?? message; businessCode = value.businessCode ?? value.code } catch { /* use safe message */ }
    throw new CustomerAccountApiError(message, response.status, businessCode)
  }
  if (response.status === 204 || response.status === 202) return undefined as T
  return response.json() as Promise<T>
}

const json = (value: unknown): RequestInit => ({ method: 'POST', body: JSON.stringify(value) })
const put = (value: unknown): RequestInit => ({ method: 'PUT', body: JSON.stringify(value) })

export const customerAccountApi = {
  loginAvailability: (value: string) => request<Availability>(`/api/v1/public/customer-account/availability/login-id?value=${encodeURIComponent(value)}`),
  emailAvailability: (value: string) => request<Availability>(`/api/v1/public/customer-account/availability/email?value=${encodeURIComponent(value)}`),
  phoneAvailability: (value: string) => request<Availability>(`/api/v1/public/customer-account/availability/phone?value=${encodeURIComponent(value)}`),
  legalDocuments: () => request<LegalDocument[]>('/api/v1/public/customer-account/legal-documents'),
  identityVerificationStatus: () => request<{ statusCode: string; isVerified: boolean }>('/api/v1/public/customer-account/identity-verification/status'),
  register: (value: unknown) => request('/api/v1/public/customer-account/register', json(value)),
  requestPasswordReset: (loginOrEmail: string) => request('/api/v1/public/customer-account/password-reset/requests', json({ loginOrEmail })),
  profile: () => request<CustomerProfile>('/api/v1/customer/account/profile'),
  updateProfile: (value: unknown) => request<CustomerProfile>('/api/v1/customer/account/profile', put(value)),
  addresses: () => request<CustomerAddress[]>('/api/v1/customer/account/addresses'),
  createAddress: (value: unknown) => request<CustomerAddress>('/api/v1/customer/account/addresses', json(value)),
  updateAddress: (id: string, value: unknown) => request<CustomerAddress>(`/api/v1/customer/account/addresses/${id}`, put(value)),
  deleteAddress: (id: string, concurrencyToken: string) => request(`/api/v1/customer/account/addresses/${id}?concurrencyToken=${encodeURIComponent(concurrencyToken)}`, { method: 'DELETE' }),
  changePassword: (value: unknown) => request('/api/v1/customer/account/password/change', json(value)),
  consents: () => request<Consent[]>('/api/v1/customer/account/consents'),
  consentHistory: () => request<ConsentHistory[]>('/api/v1/customers/me/consent-history'),
  updateConsent: (legalDocumentVersionId: string, agreed: boolean) => request('/api/v1/customer/account/consents', put({ legalDocumentVersionId, agreed })),
  withdrawal: () => request<WithdrawalDashboard>('/api/v1/customer/account/withdrawal'),
  requestWithdrawal: (scopeCode: string, reason: string) => request<WithdrawalRequest>('/api/v1/customer/account/withdrawal-requests', json({ scopeCode, reason, idempotencyKey: crypto.randomUUID() })),
  cancelWithdrawal: (id: string, reason: string, rowVersion: string) => request<WithdrawalRequest>(`/api/v1/customer/account/withdrawal-requests/${id}/cancel`, json({ reason, rowVersion, idempotencyKey: crypto.randomUUID() })),
  withdrawalReadiness: () => request<WithdrawalReadiness>('/api/v1/customers/me/withdrawal-readiness'),
  providerBlocks: () => request<ProviderBlock[]>('/api/v1/customers/me/provider-blocks'),
  blockProvider: (providerId: string, reasonCode: string | null, privateMemo: string | null) => request<ProviderBlock>('/api/v1/customers/me/provider-blocks', json({ providerId, reasonCode, privateMemo, idempotencyKey: crypto.randomUUID() })),
  releaseProviderBlock: (id: string, rowVersion: string) => request<ProviderBlock>(`/api/v1/customers/me/provider-blocks/${id}/release`, json({ rowVersion, idempotencyKey: crypto.randomUUID() })),
  mySoodalSummary: () => request<MySoodalSummary>('/api/v1/customers/me/my-soodal/summary'),
  sidos: () => request<AdministrativeArea[]>('/api/v1/administrative-areas/sidos'),
  sigungu: (parentId?: string) => request<AdministrativeArea[]>(`/api/v1/administrative-areas/sigungu${parentId ? `?parentId=${parentId}` : ''}`),
  notificationPreferences: () => request<NotificationPreference[]>('/api/v1/notifications/preferences'),
  updateNotificationPreference: (value: NotificationPreference) => request<NotificationPreference>('/api/v1/notifications/preferences', put(value)),
  notifications: (view: 'ALL' | 'UNREAD' | 'ARCHIVED' = 'ALL') => request<CustomerNotification[]>(`/api/v1/notifications?view=${view}`),
  notification: (id: string) => request<CustomerNotification>(`/api/v1/notifications/${id}`),
  unreadNotificationCount: () => request<{ count: number }>('/api/v1/notifications/unread-count'),
  readNotification: (id: string) => request(`/api/v1/notifications/${id}/read`, { method: 'POST' }),
  readAllNotifications: () => request('/api/v1/notifications/read-all', { method: 'POST' }),
  archiveNotification: (id: string) => request(`/api/v1/notifications/${id}/archive`, { method: 'POST' }),
}
