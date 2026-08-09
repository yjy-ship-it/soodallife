export interface QuoteItemInput {
  itemName: string
  description: string | null
  quantity: number
  unitText: string | null
  unitPriceAmount: number
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

export interface QuoteListItem {
  id: string
  providerName: string
  status: string
  totalAmount: number
  currencyCode: string
  submittedAt: string | null
  revisionNo: number
  validUntil: string
  isSelected: boolean
}

export interface AcceptQuoteResult {
  transactionId: string
  quoteId: string
  requestId: string
  transactionStatus: string
  agreedAmount: number
  currencyCode: string
}
