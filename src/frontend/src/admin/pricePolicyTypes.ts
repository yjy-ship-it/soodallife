export interface AdminPricePolicy {
  id: string
  policyVersion: string
  priceMethod: string
  priceTypeCode: string | null
  basePriceAmount: number
  minimumBudgetAmount: number | null
  recommendedMinAmount: number | null
  recommendedMaxAmount: number | null
  unit: string | null
  unitPriceAmount: number | null
  minimumChargeAmount: number | null
  currencyCode: string
  vatRule: string
  vatPolicyCode: string | null
  effectiveFrom: string
  effectiveTo: string | null
  effectiveStatus: 'CURRENT' | 'SCHEDULED' | 'ENDED' | 'INACTIVE'
  isCurrentlyEffective: boolean
  isReferenced: boolean
  canEdit: boolean
  isActive: boolean
  options: AdminPricePolicyOption[]
  surcharges: AdminPricePolicySurcharge[]
}

export interface AdminPricePolicyOption { id: string; optionName: string; additionalAmount: number; displayOrder: number; isActive: boolean }
export interface AdminPricePolicySurcharge { id: string; surchargeName: string; calculationTypeCode: string; amount: number | null; rate: number | null; displayOrder: number; isActive: boolean }

export interface AdminPricePolicyList {
  policies: AdminPricePolicy[]
  currentPolicyId: string | null
  allowedPriceMethods: string[]
  allowedVatRules: string[]
  supportsRecommendedMaximumPrice: boolean
  supportsExplicitActiveStatus: boolean
  supportsPriceOptions: boolean
  supportsSurcharges: boolean
}

export interface SaveAdminPricePolicyInput {
  policyVersion: string
  priceMethod: string
  basePriceAmount: number | null
  minimumBudgetAmount: number | null
  unit: string | null
  vatRule: string
  effectiveFrom: string
  effectiveTo: string | null
  isActive: boolean
}

export interface SaveAdminPricePolicyOptionInput { optionName: string; additionalAmount: number; displayOrder: number; isActive: boolean }
export interface SaveAdminPricePolicySurchargeInput { surchargeName: string; calculationTypeCode: 'AMOUNT' | 'RATE'; amount: number | null; rate: number | null; displayOrder: number; isActive: boolean }
