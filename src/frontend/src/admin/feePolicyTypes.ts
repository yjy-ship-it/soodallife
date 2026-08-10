export type FeePolicyStatus = 'CURRENT' | 'SCHEDULED' | 'ENDED' | 'INACTIVE'

export interface AdminFeePolicy {
  id: string
  policyVersion: string
  sourcePolicyCode: string | null
  policyKindCode: string
  transactionTypeCode: string
  calculationMethod: string | null
  feeAmount: number | null
  minBaseAmount: number | null
  maxBaseAmount: number | null
  rate: number | null
  monthlyAmount: number | null
  perVisitAmount: number | null
  currencyCode: string
  chargeTiming: string
  restoreRule: string | null
  effectiveFrom: string
  effectiveTo: string | null
  effectiveStatus: FeePolicyStatus
  isCurrentlyEffective: boolean
  isReferenced: boolean
  canEdit: boolean
  isActive: boolean
}

export interface AdminFeePolicyList {
  policies: AdminFeePolicy[]
  currentPolicyId: string | null
  allowedPolicyKinds: string[]
  allowedTransactionTypes: string[]
  allowedCalculationMethods: string[]
  allowedChargeTimings: string[]
  allowedCurrencies: string[]
}

export interface SaveAdminFeePolicyInput {
  policyVersion: string
  policyKindCode: string
  transactionTypeCode: string
  calculationMethod: string | null
  feeAmount: number | null
  minBaseAmount: number | null
  maxBaseAmount: number | null
  rate: number | null
  monthlyAmount: number | null
  perVisitAmount: number | null
  currencyCode: string
  chargeTiming: string
  restoreRule: string | null
  effectiveFrom: string
  effectiveTo: string | null
  isActive: boolean
}
