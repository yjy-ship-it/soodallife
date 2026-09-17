export interface QuoteItemInput {
  itemName: string
  description: string | null
  quantity: number
  unitText: string | null
  unitPriceAmount: number
  itemCategoryCode?: string | null
}

export interface ProviderQuoteTemplate {
  id: string
  name: string
  summary: string
  terms: string | null
  estimatedDurationText: string | null
  vatMode: 'INCLUDED' | 'EXCLUDED'
  items: QuoteItemInput[]
  updatedAt: string
}

export interface SaveQuoteTemplateInput {
  name: string
  summary: string
  terms: string | null
  estimatedDurationText: string | null
  vatMode: 'INCLUDED' | 'EXCLUDED'
  items: QuoteItemInput[]
}

export interface SaveQuoteRevisionInput {
  summary: string
  terms: string | null
  vatAmount: number
  estimatedDurationText: string | null
  availableStartAt: string | null
  validUntil: string
  revisionReason: string | null
  idempotencyKey: string
  items: QuoteItemInput[]
  vatMode?: 'INCLUDED' | 'EXCLUDED'
}

export interface QuoteItem extends QuoteItemInput {
  lineNo: number
  lineTotalAmount: number
  currencyCode: string
}

export interface QuoteRevision {
  id: string
  revisionNo: number
  summary: string
  terms: string | null
  subtotalAmount: number
  vatAmount: number
  totalAmount: number
  currencyCode: string
  estimatedDurationText: string | null
  availableStartAt: string | null
  validUntil: string
  revisionReason: string | null
  recordedAt: string
  items: QuoteItem[]
}

export interface QuoteDetail {
  id: string
  requestId: string
  providerName: string
  status: string
  submittedAt: string | null
  acceptedAt: string | null
  expiresAt: string | null
  canEdit: boolean
  canSubmit: boolean
  revision: QuoteRevision
  transactionId: string | null
}

export interface QuoteSubmissionReadiness {
  requestId: string
  providerId: string
  feePolicyId: string
  feePolicyVersion: string
  expectedAcceptanceFee: number
  currencyCode: string
  availableWalletBalance: number
  walletStatus: string
  canSubmit: boolean
  unavailableReason: string | null
}

export interface QuoteListItem {
  id: string
  requestId: string
  requestTitle: string
  domain: 'GENERAL' | 'INTERIOR' | 'EMERGENCY'
  categoryPath: string
  providerName: string
  status: string
  totalAmount: number
  currencyCode: string
  submittedAt: string | null
  revisionNo: number
  validUntil: string
  isSelected: boolean
  transactionId: string | null
}

export interface RatingAverage { itemId: string; itemCode: string; itemName: string; averageValue: number; ratingCount: number; minValue: number; maxValue: number }
export interface CustomerQuoteComparison {
  id: string; providerId: string; providerName: string; status: string; subtotalAmount: number; vatAmount: number; totalAmount: number; currencyCode: string; submittedAt: string | null; revisionNo: number; validUntil: string; availableStartAt: string | null; estimatedDurationText: string | null; terms: string | null; includedItems: string[]; defaultWarrantyDays: number; trustScore: number | null; trustGrade: string | null; trustEvaluationStatus: string; trustDisplay: string; reviewCount: number; publicReviewCount: number; ratingItemAverages: RatingAverage[]; providerApprovalStatus: string; serviceApprovalStatus: string; requirementsConfigured: boolean; requiredEvidenceSatisfied: boolean; isSelected: boolean
}
export interface CustomerQuoteDetail extends QuoteDetail { providerId: string; comparison: CustomerQuoteComparison }
export interface ProviderReview { id: string; bodyText: string; submittedAt: string; ratings: RatingAverage[] }
export interface PublicReviewRating { itemId:string; itemCode:string; itemName:string; ratingValue:number; minValue:number; maxValue:number; displayOrder:number }
export interface PublicReviewFile { fileId:string; fileName:string; contentType:string; displayOrder:number; downloadUrl:string|null; publicationMode:string|null }
export interface PublicReviewReply { id:string; providerName:string; bodyText:string; submittedAt:string }
export interface PublicProviderReview { id:string; providerId:string; providerName:string; customerDisplayName:string; bodyText:string; overallRating:number|null; verificationStatusCode:string; visibilityStatusCode:string; submittedAt:string; ratings:PublicReviewRating[]; files:PublicReviewFile[]; providerReply:PublicReviewReply|null }
export interface PublicProviderReviewList { totalCount:number; page:number; pageSize:number; items:PublicProviderReview[] }
export interface CustomerProviderProfile { id: string; businessName: string; introductionHtml: string | null; publicPhone: string | null; publicEmail: string | null; publicAddress: string | null; publicBlogUrl: string | null; publicWebsiteUrl: string | null; publicLogoUrl: string | null; publicPhotoUrls: string[]; approvalStatus: string; activityStatus: string; serviceApprovalStatus: string; activeServices: string[]; trustScore: number | null; trustGrade: string | null; trustEvaluationStatus: string; trustDisplay: string; completedServiceCount: number; publicReviewCount: number; ratingItemAverages: RatingAverage[]; requirementsConfigured: boolean; requiredEvidenceCount: number; approvedEvidenceCount: number; requiredEvidenceSatisfied: boolean; recentReviews: ProviderReview[] }

export interface AcceptQuoteResult {
  transactionId: string
  quoteId: string
  requestId: string
  transactionStatus: string
  agreedAmount: number
  currencyCode: string
}
