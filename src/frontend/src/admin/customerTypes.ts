export interface AdminCustomerList {
  totalCount: number
  page: number
  pageSize: number
  items: AdminCustomerListItem[]
}

export interface AdminCustomerListItem {
  id: string
  name: string
  maskedPhone: string | null
  maskedEmail: string | null
  statusCode: string
  joinedAt: string
  roles: string[]
  requestCount: number
  transactionCount: number
  lastUsedAt: string | null
}

export interface AdminCustomerDetail {
  basic: { id: string; userId: string; name: string; phone: string | null; email: string | null; statusCode: string; joinedAt: string; lastLoginAt: string | null; identityVerificationSupported: boolean; identityVerificationStatus: string; roles: string[] }
  usage: { totalRequestCount: number; inProgressRequestCount: number; completedTransactionCount: number; inProgressAfterServiceCount: number; lastUsedAt: string | null }
  addresses: { isSupported: boolean; message: string; items: Array<{ alias: string; isDefault: boolean; region: string | null; address: string; detailAddress: string | null; registeredAt: string }> }
  requests: Array<{ id: string; title: string; serviceName: string; createdAt: string; statusCode: string; areaName: string; detailAddress: string | null; quoteCount: number; hasAcceptedQuote: boolean; abuseCountExcluded: boolean; abuseExclusionReason: string | null }>
  quotes: Array<{ id: string; requestId: string; requestTitle: string; providerName: string; totalAmount: number | null; currencyCode: string; statusCode: string; submittedAt: string | null; isAccepted: boolean; acceptedAt: string | null }>
  transactions: Array<{ id: string; requestId: string; serviceName: string; providerName: string; statusCode: string; agreedAmount: number; actualAmount: number | null; currencyCode: string; startedAt: string | null; completedAt: string | null; visitInformation: string | null }>
  reviews: { isSupported: boolean; message: string }
  afterServices: Array<{ id: string; transactionId: string; subject: string; statusCode: string; receivedAt: string; completedAt: string | null; processingResult: string | null }>
  serviceHistory: Array<{ id: string; transactionId: string | null; eventTypeCode: string; title: string; summary: string; providerName: string | null; categoryName: string | null; totalAmount: number | null; currencyCode: string | null; completedAt: string | null; occurredAt: string; warrantyEndDate: string | null }>
  consents: { isSupported: boolean; message: string }
  status: { currentStatusCode: string; roleHistory: Array<{ roleCode: string; grantedAt: string; revokedAt: string | null }>; withdrawalWorkflowSupported: boolean; withdrawalMessage: string }
  managementHistory: Array<{ occurredAt: string; actionCode: string; entityType: string; resultCode: string; reason: string | null; actorRoleCode: string | null }>
}
