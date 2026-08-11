export type PublicCategory = {
  id: string
  code: string | null
  name: string
  level: 'MAJOR' | 'MIDDLE' | 'SERVICE'
  parentId: string | null
  displayOrder: number
  childCount: number
}

export type PublicPriceSummary = {
  priceMethod: string
  priceDisplayLabel: string
  referenceAmount: number | null
  recommendedMinAmount: number | null
  recommendedMaxAmount: number | null
  workUnit: string | null
  currency: string
  vatDisplayText: string
  guidanceText: string
}

export type PublicServiceSummary = {
  id: string
  code: string | null
  name: string
  majorId: string
  majorName: string
  middleId: string
  middleName: string
  description: string | null
  price: PublicPriceSummary | null
}

export type PublicServiceDetail = PublicServiceSummary & {
  onsiteRequirement: string
  emergencyRequestAllowed: boolean
  subscriptionAvailable: boolean
  defaultWarrantyDays: number
  requestGuide: string
  providerRequirementGuide: string | null
  requestFields: Array<{
    id: string
    label: string
    inputType: string
    required: boolean
    unit: string | null
    displayOrder: number
  }>
}

export type PublicContent = {
  id: string
  contentTypeCode: string
  title: string
  bodyText: string | null
  questionText: string | null
  answerText: string | null
  destinationTypeCode: string
  destinationValue: string | null
}

export type PublicPromotion = {
  campaignId: string
  creativeId: string
  title: string
  subtitle: string | null
  bodyText: string | null
  buttonText: string | null
  destinationTypeCode: string
  destinationValue: string | null
}
