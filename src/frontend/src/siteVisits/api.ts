import type { SiteVisitProposal,SaveSiteVisitProposal } from './types'

async function read<T>(response:Response):Promise<T>{
  if(!response.ok){let message='방문견적 요청을 처리하지 못했습니다.';try{const body=await response.json() as {message?:string};message=body.message??message}catch{/* noop */}throw new Error(message)}
  return response.json() as Promise<T>
}
const json=(path:string,body:unknown)=>fetch(path,{method:'POST',credentials:'include',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}).then(read<SiteVisitProposal>)
export const providerList=(requestId:string)=>fetch(`/api/v1/providers/me/requests/${requestId}/site-visits`,{credentials:'include'}).then(read<SiteVisitProposal[]>)
export const customerList=(requestId:string)=>fetch(`/api/v1/customer/requests/${requestId}/site-visits`,{credentials:'include'}).then(read<SiteVisitProposal[]>)
export const save=(requestId:string,input:SaveSiteVisitProposal)=>json(`/api/v1/providers/me/requests/${requestId}/site-visits`,input)
export const accept=(id:string,detailAddress:string,rowVersion:string)=>json(`/api/v1/site-visits/${id}/accept`,{detailAddress,termsAccepted:true,idempotencyKey:crypto.randomUUID(),rowVersion})
export const reject=(id:string,note:string|null,rowVersion:string)=>json(`/api/v1/site-visits/${id}/reject`,{action:'REJECT',note,idempotencyKey:crypto.randomUUID(),rowVersion})
export const cancel=(id:string,note:string|null,rowVersion:string)=>json(`/api/v1/site-visits/${id}/cancel`,{action:'CANCEL',note,idempotencyKey:crypto.randomUUID(),rowVersion})
export const progress=(id:string,action:string,rowVersion:string,note:string|null=null)=>json(`/api/v1/site-visits/${id}/progress`,{action,note,idempotencyKey:crypto.randomUUID(),rowVersion})
export const reportPayment=(id:string,rowVersion:string,memo:string|null)=>json(`/api/v1/site-visits/${id}/payment-report`,{memo,idempotencyKey:crypto.randomUUID(),rowVersion})
export const decidePayment=(id:string,rowVersion:string,decision:string,reason:string|null)=>json(`/api/v1/site-visits/${id}/payment-decision`,{decision,reason,idempotencyKey:crypto.randomUUID(),rowVersion})
export const noShow=(id:string,rowVersion:string,subjectRole:string,evidenceNote:string|null)=>json(`/api/v1/site-visits/${id}/no-show`,{subjectRole,contactAttempts:2,evidenceNote,idempotencyKey:crypto.randomUUID(),rowVersion})
export const dispute=(id:string,rowVersion:string,reason:string)=>json(`/api/v1/site-visits/${id}/no-show/dispute`,{reason,idempotencyKey:crypto.randomUUID(),rowVersion})
