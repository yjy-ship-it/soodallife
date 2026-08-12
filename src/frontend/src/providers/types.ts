export interface ProviderProfile {
  id: string
  businessName: string
  representativeName: string | null
  contactName: string | null
  phone: string | null
  email: string | null
  businessRegistrationNumber: string | null
  businessAddress: string | null
  businessTypeText: string | null
  businessItemText: string | null
  introduction: string | null
  providerType: string | null
  approvalStatus: string
  activityStatus: string
  trustScore: number | null
  trustDisplayStatus: string
  rejectionReason: string | null
  concurrencyToken: string
}

export interface ProviderServiceCategory {
  categoryId: string
  categoryPath: string
  status: string
  approvalStatus: string
  decisionReason: string | null
  requiredRequirementCount: number
  approvedRequirementCount: number
}

export interface ProviderEvidenceType { documentTypeId: string; code: string; name: string; isRequired: boolean }
export interface ProviderRequirement { verificationId: string; assignmentId: string; serviceCategoryId: string; categoryPath: string; requirementCode: string; requirementName: string; requirementType: string; isRequired: boolean; verificationRequired: boolean; expiryCheckRequired: boolean; minimumValidDays: number | null; verificationStatus: string; documentId: string | null; documentName: string | null; expiresAt: string | null; rejectionReason: string | null; acceptedEvidenceTypes: ProviderEvidenceType[] }
export interface ProviderDocumentType { id: string; code: string; name: string; description: string | null }
export interface ProviderDocument { id: string; fileId: string; documentTypeCode: string; documentTypeName: string; originalFileName: string; sizeBytes: number; contentType: string; verificationStatus: string; malwareStatus: string; issuedAt: string | null; expiresAt: string | null; publicNote: string | null; createdAt: string }
export interface ProviderDashboard { approvalStatus: string; activityStatus: string; registeredServiceCount: number; approvedServiceCount: number; pendingServiceCount: number; rejectedServiceCount: number; activeAreaCount: number; requiredEvidenceCount: number; submittedEvidenceCount: number; approvedEvidenceCount: number; nextActions: string[]; rejectionReason: string | null }
export interface ProviderLegalDocument { id: string; versionId: string; code: string; requirementCode: string; title: string; content: string; version: number; effectiveFrom: string; effectiveTo: string | null; isPlaceholder: boolean }

export interface ProviderArea {
  id: string
  name: string
  areaCode: string
}

export interface ProviderServiceArea {
  serviceCategoryId: string
  categoryPath: string
  areas: ProviderArea[]
}

export interface MatchedRequestListItem {
  requestId: string
  categoryPath: string
  areaName: string
  summary: string
  desiredAt: string | null
  dispatchedAt: string
  dispatchStatus: string
  requestStatus: string
}

export interface MatchedRequestAnswer {
  fieldId: string
  label: string
  inputType: string
  value: unknown
  isMasked: boolean
}

export interface MatchedRequestDetail extends MatchedRequestListItem {
  title: string
  description: string | null
  isUrgent: boolean
  expiresAt: string
  answers: MatchedRequestAnswer[]
}
