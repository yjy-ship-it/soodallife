export interface SettlementSummary {
  walletAvailableBalance: number
  walletReservedBalance: number
  reservedQuoteFeeCount: number
  reservedQuoteFeeAmount: number
  capturedQuoteFeeAmount: number
  failedSubscriptionPaymentCount: number
  completedSubscriptionPaymentAmount: number
  pendingMonthlySettlementCount: number
  pendingMonthlySettlementAmount: number
  pendingPayoutCount: number
  pendingPayoutAmount: number
  pendingAdvertisingFeeCount: number
  pendingAdvertisingFeeAmount: number
  openRefundAdjustmentCount: number
  openRefundAdjustmentAmount: number
}

export interface QuoteFeeOperation { id:string;quoteId:string;providerId:string;providerName:string;amount:number;currencyCode:string;statusCode:string;reservedAt:string;capturedAt:string|null;releasedAt:string|null;releaseReasonCode:string|null }
export interface SubscriptionPaymentOperation { id:string;contractId:string;providerName:string;billingPeriodStart:string;billingPeriodEnd:string;requestedAmount:number;currencyCode:string;statusCode:string;requestedAt:string;completedAt:string|null;failureReason:string|null }
export interface MonthlySettlementOperation { id:string;providerId:string;providerName:string;year:number;month:number;statusCode:string;grossTotal:number;feeTotal:number;adjustmentTotal:number;netTotal:number;itemCount:number;approvedAt:string|null;paidAt:string|null }
export interface PayoutOperation { id:string;monthlySettlementId:string;providerId:string;providerName:string;requestedAmount:number;approvedAmount:number|null;statusCode:string;requestedAt:string;completedAt:string|null;bankTransferReference:string|null }
export interface AdvertisingFeeOperation { id:string;campaignName:string;providerId:string;providerName:string;feeAmount:number;currencyCode:string;statusCode:string;feeStatusCode:string;autoRenewEnabled:boolean;submittedAt:string;publishedAt:string|null;nextRenewalAt:string|null }
export interface RefundAdjustmentOperation { id:string;sourceCode:string;providerId:string|null;partyName:string;typeCode:string;requestedAmount:number;approvedAmount:number|null;statusCode:string;reason:string;requestedAt:string;completedAt:string|null }

export interface SettlementOperationsDashboard {
  generatedAt:string
  summary:SettlementSummary
  quoteFees:QuoteFeeOperation[]
  subscriptionPayments:SubscriptionPaymentOperation[]
  monthlySettlements:MonthlySettlementOperation[]
  payouts:PayoutOperation[]
  advertisingFees:AdvertisingFeeOperation[]
  refundAdjustments:RefundAdjustmentOperation[]
}

export interface UnifiedLedgerItem { sourceCode:string;id:string|null;occurredAt:string;entryTypeCode:string;amount:number|null;currencyCode:string;partyName:string;statusCode:string;referenceType:string|null;referenceId:string|null;reason:string|null;actorRoleCode:string|null }
export interface UnifiedLedger { totalCount:number;page:number;pageSize:number;items:UnifiedLedgerItem[] }
