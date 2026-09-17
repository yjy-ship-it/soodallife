export type PublicArea = { id: string; name: string; areaCode?: string; parentId: string | null; parentName: string | null }

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
  vatPolicyCode: 'INCLUDED' | 'EXCLUDED' | 'EXEMPT' | 'UNDETERMINED'
  vatDisplayText: string
  vatAmount: number | null
  totalAmount: number | null
  guidanceText: string
}

export type PublicServiceSummary = {
  id: string
  code: string | null
  slug: string | null
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
  coverageTypeCode: 'LOCAL_ONLY' | 'NATIONWIDE_REMOTE' | 'NATIONWIDE_DELIVERY' | 'NATIONWIDE_NETWORK' | 'FLEXIBLE'
  requiresServiceAddress: boolean
  emergencyRequestAllowed: boolean
  subscriptionAvailable: boolean
  defaultWarrantyDays: number
  requestGuide: string
  providerRequirementGuide: string | null
  seoTitle: string
  seoDescription: string
  searchKeywordsText: string | null
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
  audienceTypeCode: string
  title: string
  bodyText: string | null
  questionText: string | null
  answerText: string | null
  destinationTypeCode: string
  destinationValue: string | null
  displayOrder: number
  versionNo: number
  publishedAt: string
}

export type PublicPromotion = {
  campaignId: string
  providerId: string | null
  creativeId: string
  title: string
  subtitle: string | null
  bodyText: string | null
  mediaId: string | null
  altText: string | null
  buttonText: string | null
  destinationTypeCode: string
  destinationValue: string | null
}

export type PublicActivityItem = {
  id: string
  eventTypeCode: string
  statusLabel: string
  domainCode: 'GENERAL' | 'CARE' | 'INTERIOR' | 'EMERGENCY'
  domainLabel: string
  serviceId: string
  serviceName: string
  serviceCode: string | null
  regionName: string
  occurredAt: string
  requestedAt: string | null
  requestTitle: string
  requestSummary: string | null
  requestDetails: Array<{ label: string; value: string }>
  quoteCount: number | null
  quoteAmounts: Array<{ amount: number; currencyCode: string; submittedAt: string }>
  servicePath: string | null
  progressSteps: string[]
  activeStep: number
}

export type PublicActivityFeed = {
  items: PublicActivityItem[]
  serverTime: string
  nextRefreshSeconds: number
  privacyNotice: string
}

export type LivingHome = {
  dailyChecks: Array<{ id:string;seasonLabel:string;title:string;body:string;actionLabel:string;servicePath:string }>
  localInsights: Array<{ serviceName:string;regionName:string;completedCount:number;typicalMinAmount:number|null;typicalMaxAmount:number|null;currencyCode:string;samplePeriodDays:number;servicePath:string;evidenceLabel:string }>
  maintenanceCalendar: Array<{ id:string;title:string;detail:string;dueDate:string;statusCode:'OVERDUE'|'DUE_SOON'|'UPCOMING';sourceLabel:string;servicePath:string }>
  workStories: Array<{ id:string;serviceName:string;regionName:string;providerName:string;reviewText:string;rating:number|null;completedAt:string;servicePath:string }>
  expertAnswers: Array<{ id:string;question:string;answer:string;providerName:string;serviceName:string;answeredAt:string;servicePath:string }>
  regionName:string
  isPersonalized:boolean
  generatedAt:string
  privacyNotice:string
}
