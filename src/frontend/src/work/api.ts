import type { ApiErrorBody } from '../requests/types'
import type { AppointmentChange, CompletionConfirmation, CompletionEvidence, CustomerAfterService, CustomerDispute, CustomerReport, CustomerReportType, DirectPaymentContext, RatingItem, ReviewResponse, ServiceHistoryDetail, ServiceHistoryItem, TransactionAppointment, TransactionCancellation, TransactionDirectPayment, WorkCompletionRevision, WorkTransactionDetail, WorkTransactionListItem } from './types'
import { quoteSummaryText,quoteTermsText } from '../quotes/displayText'

const normalizeQuoteText=(value:WorkTransactionDetail):WorkTransactionDetail=>({...value,acceptedQuote:{...value.acceptedQuote,summary:quoteSummaryText(value.acceptedQuote.summary,value.acceptedQuote.items,value.requestTitle),terms:quoteTermsText(value.acceptedQuote.terms)}})

async function read<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let body: ApiErrorBody | null = null
    try { body = await response.json() as ApiErrorBody } catch { /* generic error below */ }
    throw new Error(body?.message ?? '요청을 처리하지 못했습니다.')
  }
  if (response.status === 204) return null as T
  const text = await response.text()
  if (!text.trim()) return null as T
  try { return JSON.parse(text) as T }
  catch { throw new Error('서버 응답 형식을 확인하지 못했습니다. 잠시 후 다시 시도해 주세요.') }
}
export const getAppointment = (id: string) => fetch(`/api/v1/transactions/${id}/appointment`, { credentials: 'include' }).then(read<TransactionAppointment | null>)
export const proposeAppointment = (id:string,scheduledStartAt:string,scheduledEndAt:string|null,estimatedDurationMinutes:number|null,memo:string|null)=>json('POST',`/api/v1/transactions/${id}/appointment-proposals`,{scheduledStartAt,scheduledEndAt,estimatedDurationMinutes,memo,idempotencyKey:crypto.randomUUID()}).then(read<TransactionAppointment>)
export const decideAppointment=(id:string,decision:'APPROVE'|'REJECT',reason:string|null,rowVersion:string)=>json('POST',`/api/v1/transactions/${id}/appointment/decision`,{decision,reason,idempotencyKey:crypto.randomUUID(),rowVersion}).then(read<TransactionAppointment>)
export const requestAppointmentChange=(id:string,requestedStartAt:string|null,requestedEndAt:string|null,reason:string,changeType:'RESCHEDULE'|'CANCEL'='RESCHEDULE')=>json('POST',`/api/v1/transactions/${id}/appointment-change-requests`,{requestedStartAt,requestedEndAt,reason,idempotencyKey:crypto.randomUUID(),changeType}).then(read<AppointmentChange>)
export const decideAppointmentChange=(id:string,requestId:string,decision:'APPROVE'|'REJECT',note:string|null,rowVersion:string)=>json('POST',`/api/v1/transactions/${id}/appointment-change-requests/${requestId}/decision`,{decision,note,idempotencyKey:crypto.randomUUID(),rowVersion}).then(read<AppointmentChange>)
export const getCancellationRequests=(id:string)=>fetch(`/api/v1/transactions/${id}/cancellation-requests`,{credentials:'include'}).then(read<TransactionCancellation[]>)
export const requestTransactionCancellation=(id:string,reason:string)=>json('POST',`/api/v1/transactions/${id}/cancellation-requests`,{reason,idempotencyKey:crypto.randomUUID()}).then(read<TransactionCancellation>)
export const decideTransactionCancellation=(id:string,requestId:string,decision:'APPROVE'|'REJECT',note:string|null,rowVersion:string)=>json('POST',`/api/v1/transactions/${id}/cancellation-requests/${requestId}/decision`,{decision,note,idempotencyKey:crypto.randomUUID(),rowVersion}).then(read<TransactionCancellation>)
export const getRatingItems=()=>fetch('/api/v1/customers/me/review-rating-items',{credentials:'include'}).then(read<RatingItem[]>)
export const createReview=(id:string,bodyText:string,ratings:Array<{ratingItemId:string;ratingValue:number}>,fileIds:string[])=>json('POST',`/api/v1/customers/me/transactions/${id}/review`,{bodyText,ratings,fileIds,idempotencyKey:crypto.randomUUID()}).then(read<ReviewResponse>)
export const updateReview=(reviewId:string,bodyText:string,ratings:Array<{ratingItemId:string;ratingValue:number}>,fileIds:string[])=>json('PUT',`/api/v1/customers/me/reviews/${reviewId}`,{bodyText,ratings,fileIds,idempotencyKey:crypto.randomUUID()}).then(read<ReviewResponse>)
export async function uploadReviewFile(file:File){const form=new FormData();form.append('file',file);return fetch('/api/v1/customers/me/review-files',{method:'POST',credentials:'include',body:form}).then(read<{fileId:string}>)}
export const getMyReviews=()=>fetch('/api/v1/customers/me/reviews',{credentials:'include'}).then(read<ReviewResponse[]>)
export const getDisputes=()=>fetch('/api/v1/customers/me/disputes',{credentials:'include'}).then(read<CustomerDispute[]>)
export const getDispute=(id:string)=>fetch(`/api/v1/customers/me/disputes/${id}`,{credentials:'include'}).then(read<CustomerDispute>)
export async function uploadDisputeEvidence(id:string,file:File,description:string){const form=new FormData();form.append('description',description);form.append('file',file);return fetch(`/api/v1/customers/me/disputes/${id}/evidence`,{method:'POST',credentials:'include',body:form}).then(read<CustomerDispute['evidence'][number]>)}
export const getServiceHistory=()=>fetch('/api/v1/customers/me/service-history',{credentials:'include'}).then(read<ServiceHistoryItem[]>)
export const getServiceHistoryDetail=(id:string)=>fetch(`/api/v1/customers/me/service-history/${id}`,{credentials:'include'}).then(read<ServiceHistoryDetail>)
export const getAfterServices=()=>fetch('/api/v1/customers/me/after-services',{credentials:'include'}).then(read<CustomerAfterService[]>)
export const getAfterService=(id:string)=>fetch(`/api/v1/after-services/${id}`,{credentials:'include'}).then(read<CustomerAfterService>)
export const createAfterService=(transactionId:string,input:{subject:string;description:string;requestDetails:string|null;desiredVisitAt:string|null})=>json('POST',`/api/v1/customers/me/transactions/${transactionId}/after-services`,{...input,idempotencyKey:crypto.randomUUID()}).then(read<CustomerAfterService>)
export async function uploadAfterServiceEvidence(id:string,file:File,description:string){const form=new FormData();form.append('role','CUSTOMER_EVIDENCE');form.append('description',description);form.append('file',file);return fetch(`/api/v1/after-services/${id}/evidence`,{method:'POST',credentials:'include',body:form}).then(read<CustomerAfterService['evidence'][number]>)}
export const convertAfterServiceToDispute=(id:string,input:{subject:string;reason:string;requestedResolution:string})=>json('POST',`/api/v1/customers/me/after-services/${id}/dispute`,{...input,idempotencyKey:crypto.randomUUID()}).then(read<CustomerDispute>)
export const getReportTypes=()=>fetch('/api/v1/customers/me/report-types',{credentials:'include'}).then(read<CustomerReportType[]>)
export const getReports=()=>fetch('/api/v1/customers/me/reports',{credentials:'include'}).then(read<CustomerReport[]>)
export const getReport=(id:string)=>fetch(`/api/v1/customers/me/reports/${id}`,{credentials:'include'}).then(read<CustomerReport>)
export const createReport=(input:{targetType:string;targetId:string;reportTypeId:string;description:string})=>json('POST','/api/v1/customers/me/reports',{...input,idempotencyKey:crypto.randomUUID()}).then(read<CustomerReport>)
export async function uploadReportEvidence(id:string,file:File,description:string){const form=new FormData();form.append('description',description);form.append('file',file);return fetch(`/api/v1/customers/me/reports/${id}/evidence`,{method:'POST',credentials:'include',body:form}).then(read<CustomerReport['evidence'][number]>)}
const json = (method: string, path: string, body?: unknown) => fetch(path, { method, credentials: 'include', headers: body === undefined ? undefined : { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body) })
export const getProviderTransactions = () => fetch('/api/v1/providers/me/transactions', { credentials: 'include' }).then(read<WorkTransactionListItem[]>)
export const getProviderTransaction = (id: string) => fetch(`/api/v1/providers/me/transactions/${id}`, { credentials: 'include' }).then(read<WorkTransactionDetail>).then(normalizeQuoteText)
export const getCustomerTransactions = () => fetch('/api/v1/customers/me/transactions', { credentials: 'include' }).then(read<WorkTransactionListItem[]>)
export const getCustomerTransaction = (id: string) => fetch(`/api/v1/customers/me/transactions/${id}`, { credentials: 'include' }).then(read<WorkTransactionDetail>).then(normalizeQuoteText)
export const startTransaction = (id: string) => json('POST', `/api/v1/transactions/${id}/start`).then(read<WorkTransactionDetail>).then(normalizeQuoteText)
export const saveCompletionDraft = (id: string, workSummary: string, actualAmount: number, revisionReason: string | null, idempotencyKey: string) => json('POST', `/api/v1/transactions/${id}/completions/drafts`, { workSummary, actualAmount, revisionReason, idempotencyKey }).then(read<WorkCompletionRevision>)
export const submitCompletion = (id: string) => json('POST', `/api/v1/transactions/${id}/completions/submit`).then(read<WorkCompletionRevision>)
export const confirmCompletion = (id: string, completionRevisionId: string, result: string, comment: string | null, idempotencyKey: string) => json('POST', `/api/v1/transactions/${id}/confirm-completion`, { completionRevisionId, result, comment, idempotencyKey }).then(read<CompletionConfirmation>)
export const getDirectPayment=(id:string)=>fetch(`/api/v1/transactions/${id}/direct-payment`,{credentials:'include'}).then(read<DirectPaymentContext|null>)
export async function registerDirectPayment(id:string,input:{amount:number;paymentMethod:string;paidAt:string;note:string;transactionRowVersion:string;evidence:File|null}){const form=new FormData();form.append('amount',String(input.amount));form.append('paymentMethod',input.paymentMethod);form.append('paidAt',input.paidAt);form.append('note',input.note);form.append('transactionRowVersion',input.transactionRowVersion);form.append('idempotencyKey',crypto.randomUUID());if(input.evidence)form.append('evidence',input.evidence);return fetch(`/api/v1/transactions/${id}/direct-payment`,{method:'POST',credentials:'include',body:form}).then(read<TransactionDirectPayment>)}
export const decideDirectPayment=(transactionId:string,paymentId:string,decision:'CONFIRM'|'REJECT',reason:string|null,rowVersion:string)=>json('POST',`/api/v1/transactions/${transactionId}/direct-payment/${paymentId}/decision`,{decision,reason,rowVersion,idempotencyKey:crypto.randomUUID()}).then(read<TransactionDirectPayment>)
export async function uploadEvidence(id: string, roleCode: string, description: string, file: File) {
  const form = new FormData(); form.append('roleCode', roleCode); form.append('description', description); form.append('file', file)
  return fetch(`/api/v1/transactions/${id}/completion-evidence`, { method: 'POST', credentials: 'include', body: form }).then(read<CompletionEvidence>)
}
export const deleteEvidence = (id: string, fileId: string) => json('DELETE', `/api/v1/transactions/${id}/completion-evidence/${fileId}`).then(read<WorkCompletionRevision>)
