export interface ProviderProfile {
  id: string
  businessName: string
  approvalStatus: string
  activityStatus: string
}

export interface ProviderServiceCategory {
  categoryId: string
  categoryPath: string
  status: string
}

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
