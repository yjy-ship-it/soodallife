import type { ApiErrorBody } from '../requests/types'
import type { AppointmentChange, CompletionConfirmation, CompletionEvidence, CustomerDispute, RatingItem, ReviewResponse, TransactionAppointment, WorkCompletionRevision, WorkTransactionDetail, WorkTransactionListItem } from './types'

async function read<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let body: ApiErrorBody | null = null
    try { body = await response.json() as ApiErrorBody } catch { /* generic error below */ }
    throw new Error(body?.message ?? '요청을 처리하지 못했습니다.')
  }
  return await response.json() as T
}
export const getAppointment = (id: string) => fetch(`/api/v1/transactions/${id}/appointment`, { credentials: 'include' }).then(read<TransactionAppointment | null>)
export const createAppointment = (id:string,scheduledStartAt:string,scheduledEndAt:string|null,estimatedDurationMinutes:number|null,customerMemo:string|null)=>json('POST',`/api/v1/customers/me/transactions/${id}/appointment`,{scheduledStartAt,scheduledEndAt,estimatedDurationMinutes,customerMemo}).then(read<TransactionAppointment>)
export const requestAppointmentChange=(id:string,requestedStartAt:string,requestedEndAt:string|null,reason:string)=>json('POST',`/api/v1/customers/me/transactions/${id}/appointment-change-requests`,{requestedStartAt,requestedEndAt,reason,idempotencyKey:crypto.randomUUID()}).then(read<AppointmentChange>)
export const getRatingItems=()=>fetch('/api/v1/customers/me/review-rating-items',{credentials:'include'}).then(read<RatingItem[]>)
export const createReview=(id:string,bodyText:string,ratings:Array<{ratingItemId:string;ratingValue:number}>,fileIds:string[])=>json('POST',`/api/v1/customers/me/transactions/${id}/review`,{bodyText,ratings,fileIds,idempotencyKey:crypto.randomUUID()}).then(read<ReviewResponse>)
export async function uploadReviewFile(file:File){const form=new FormData();form.append('file',file);return fetch('/api/v1/customers/me/review-files',{method:'POST',credentials:'include',body:form}).then(read<{fileId:string}>)}
export const getMyReviews=()=>fetch('/api/v1/customers/me/reviews',{credentials:'include'}).then(read<ReviewResponse[]>)
export const getDisputes=()=>fetch('/api/v1/customers/me/disputes',{credentials:'include'}).then(read<CustomerDispute[]>)
export const getDispute=(id:string)=>fetch(`/api/v1/customers/me/disputes/${id}`,{credentials:'include'}).then(read<CustomerDispute>)
const json = (method: string, path: string, body?: unknown) => fetch(path, { method, credentials: 'include', headers: body === undefined ? undefined : { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body) })
export const getProviderTransactions = () => fetch('/api/v1/providers/me/transactions', { credentials: 'include' }).then(read<WorkTransactionListItem[]>)
export const getProviderTransaction = (id: string) => fetch(`/api/v1/providers/me/transactions/${id}`, { credentials: 'include' }).then(read<WorkTransactionDetail>)
export const getCustomerTransactions = () => fetch('/api/v1/customers/me/transactions', { credentials: 'include' }).then(read<WorkTransactionListItem[]>)
export const getCustomerTransaction = (id: string) => fetch(`/api/v1/customers/me/transactions/${id}`, { credentials: 'include' }).then(read<WorkTransactionDetail>)
export const startTransaction = (id: string) => json('POST', `/api/v1/transactions/${id}/start`).then(read<WorkTransactionDetail>)
export const saveCompletionDraft = (id: string, workSummary: string, actualAmount: number, revisionReason: string | null, idempotencyKey: string) => json('POST', `/api/v1/transactions/${id}/completions/drafts`, { workSummary, actualAmount, revisionReason, idempotencyKey }).then(read<WorkCompletionRevision>)
export const submitCompletion = (id: string) => json('POST', `/api/v1/transactions/${id}/completions/submit`).then(read<WorkCompletionRevision>)
export const confirmCompletion = (id: string, completionRevisionId: string, result: string, comment: string | null, idempotencyKey: string) => json('POST', `/api/v1/transactions/${id}/confirm-completion`, { completionRevisionId, result, comment, idempotencyKey }).then(read<CompletionConfirmation>)
export async function uploadEvidence(id: string, roleCode: string, description: string, file: File) {
  const form = new FormData(); form.append('roleCode', roleCode); form.append('description', description); form.append('file', file)
  return fetch(`/api/v1/transactions/${id}/completion-evidence`, { method: 'POST', credentials: 'include', body: form }).then(read<CompletionEvidence>)
}
