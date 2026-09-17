import type { ApiErrorBody } from '../requests/types'
import type { AcceptQuoteResult, CustomerProviderProfile, CustomerQuoteComparison, CustomerQuoteDetail, ProviderQuoteTemplate, PublicProviderReviewList, QuoteDetail, QuoteListItem, QuoteSubmissionReadiness, SaveQuoteRevisionInput, SaveQuoteTemplateInput } from './types'

export class QuoteApiError extends Error {
  readonly status: number
  readonly businessCode?: string
  readonly fieldErrors?: Record<string, string[]>

  constructor(message: string, status: number, businessCode?: string, fieldErrors?: Record<string, string[]>) {
    super(message)
    this.status = status
    this.businessCode = businessCode
    this.fieldErrors = fieldErrors
  }
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let body: ApiErrorBody | null = null
    try { body = (await response.json()) as ApiErrorBody } catch { /* generic response below */ }
    throw new QuoteApiError(body?.message ?? '견적 요청을 처리하지 못했습니다.', response.status, body?.businessCode, body?.fieldErrors)
  }
  return (await response.json()) as T
}

const json = (method: string, path: string, body?: unknown) => fetch(path, {
  method,
  credentials: 'include',
  headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
  body: body === undefined ? undefined : JSON.stringify(body),
})

export async function getProviderQuote(requestId: string): Promise<QuoteDetail | null> {
  const response = await fetch(`/api/v1/providers/me/requests/${requestId}/quote`, { credentials: 'include' })
  if (response.status === 404) return null
  return readJson<QuoteDetail>(response)
}
export const getQuoteSubmissionReadiness = (requestId:string) =>
  fetch(`/api/v1/providers/me/requests/${requestId}/quote-submission-readiness`, { credentials:'include' }).then(readJson<QuoteSubmissionReadiness>)
export const getProviderQuotes = () =>
  fetch('/api/v1/providers/me/quotes', { credentials: 'include' }).then(readJson<QuoteListItem[]>)
export const getQuoteTemplates = () =>
  fetch('/api/v1/providers/me/quote-templates', { credentials: 'include' }).then(readJson<ProviderQuoteTemplate[]>)
export const saveQuoteTemplate = (input: SaveQuoteTemplateInput) =>
  json('POST', '/api/v1/providers/me/quote-templates', input).then(readJson<ProviderQuoteTemplate>)
export const deleteQuoteTemplate = (templateId: string) =>
  json('DELETE', `/api/v1/providers/me/quote-templates/${templateId}`).then(readJson<boolean>)

export const createProviderQuote = (requestId: string, input: SaveQuoteRevisionInput) =>
  json('POST', `/api/v1/requests/${requestId}/quotes`, input).then(readJson<QuoteDetail>)
export const addQuoteRevision = (quoteId: string, input: SaveQuoteRevisionInput) =>
  json('POST', `/api/v1/quotes/${quoteId}/revisions`, input).then(readJson<QuoteDetail>)
export const submitQuote = (quoteId: string) =>
  json('POST', `/api/v1/quotes/${quoteId}/submit`).then(readJson<QuoteDetail>)
export const getCustomerQuotes = (requestId: string) =>
  fetch(`/api/v1/requests/${requestId}/quotes`, { credentials: 'include' }).then(readJson<CustomerQuoteComparison[]>)
export const getCustomerQuote = (quoteId: string) =>
  fetch(`/api/v1/quotes/${quoteId}`, { credentials: 'include' }).then(readJson<CustomerQuoteDetail>)
export const getCustomerProviderProfile = (providerId: string, requestId: string) =>
  fetch(`/api/v1/customer/providers/${providerId}?requestId=${encodeURIComponent(requestId)}`, { credentials: 'include' }).then(readJson<CustomerProviderProfile>)
export const getPublicProviderReviews = (providerId:string) =>
  fetch(`/api/v1/providers/${providerId}/reviews?page=1&pageSize=100`, { credentials:'include' }).then(readJson<PublicProviderReviewList>)
export const acceptQuote = (quoteId: string, detailAddress: string) =>
  json('POST', `/api/v1/quotes/${quoteId}/accept`, { detailAddress }).then(readJson<AcceptQuoteResult>)
