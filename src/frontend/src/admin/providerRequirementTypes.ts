export type ProviderRequirementStatus = 'CURRENT' | 'SCHEDULED' | 'ENDED' | 'INACTIVE'

export interface AdminProviderRequirement {
  id: string
  policyVersion: string
  qualificationAndLicenseRequirement: string
  insuranceRequirement: string
  safetyGradeCode: string
  effectiveFrom: string
  effectiveTo: string | null
  effectiveStatus: ProviderRequirementStatus
  isCurrentlyEffective: boolean
  isActive: boolean
  structuredRequirements: AdminCategoryProviderRequirement[]
}

export interface AdminCategoryProviderRequirementEvidence { documentTypeId: string; code: string; name: string; isRequired: boolean; displayOrder: number }
export interface AdminCategoryProviderRequirement {
  id: string
  requirementDefinitionId: string
  requirementTypeCode: string
  requirementCode: string
  requirementName: string
  isRequired: boolean
  verificationRequired: boolean
  expiryCheckRequired: boolean
  minimumValidDays: number | null
  displayOrder: number
  isActive: boolean
  evidenceTypes: AdminCategoryProviderRequirementEvidence[]
}

export interface SaveAdminCategoryProviderRequirementInput {
  requirementDefinitionId: string
  isRequired: boolean
  verificationRequired: boolean
  expiryCheckRequired: boolean
  minimumValidDays: number | null
  displayOrder: number
  isActive: boolean
}

export interface AdminProviderRequirementList {
  policies: AdminProviderRequirement[]
  currentPolicyId: string | null
  hasHistory: boolean
  supportsStructuredRequirements: boolean
  supportsEvidenceValidityRules: boolean
  canEdit: boolean
}
