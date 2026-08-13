export type ExitBlocker={code:string;label:string;count:number;blocksCompletion:boolean}
export type ExitWallet={walletId:string;currencyCode:string;availableBalance:number;reservedBalance:number;statusCode:string;pendingRefundAmount:number;pendingChargeCount:number;pendingRefundCount:number;pendingFeeRestoreCount:number;refundRequired:boolean;reservedBalanceBlocks:boolean}
export type ExitReadiness={canComplete:boolean;recommendedStatus:string;blockers:ExitBlocker[];wallet:ExitWallet;activeWorkCount:number;openDisputeCount:number;activeChatAccessCount:number;accountClosurePolicyRequired:boolean;guidance:string;retentionPolicyStatus:string}
export type ExitRequest={id:string;requestType:string;reason:string;status:string;reviewStatus:string;requestedAt:string;reviewedAt:string|null;decisionReason:string|null;completedAt:string|null;refundRequestId:string|null;refundStatus:string|null;rowVersion:string;readiness:ExitReadiness}
export type ProviderExitDashboard={providerName:string;approvalStatus:string;activityStatus:string;hasCustomerRole:boolean;request:ExitRequest|null;readiness:ExitReadiness}
export type AdminExitItem={id:string;providerId:string;providerName:string;requestType:string;status:string;reviewStatus:string;requestedAt:string;activeWorkCount:number;openDisputeCount:number;availableBalance:number;reservedBalance:number;refundRequired:boolean;refundStatus:string|null}
export type AdminExitList={totalCount:number;page:number;pageSize:number;items:AdminExitItem[]}
export type AdminExitDetail={id:string;providerId:string;providerName:string;requestType:string;reason:string;status:string;reviewStatus:string;requestedAt:string;reviewedAt:string|null;decisionReason:string|null;completedAt:string|null;hasCustomerRole:boolean;refundRequestId:string|null;refundStatus:string|null;rowVersion:string;readiness:ExitReadiness}

async function request<T>(path:string,init?:RequestInit):Promise<T>{const response=await fetch(path,{credentials:'include',...init});if(!response.ok){const body=await response.json().catch(()=>null) as {message?:string}|null;throw new Error(body?.message??'요청을 처리하지 못했습니다.')}return response.json() as Promise<T>}
const body=(value:unknown):RequestInit=>({method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(value)})
export const getProviderExit=()=>request<ProviderExitDashboard>('/api/v1/providers/me/exit')
export const createProviderExit=(requestType:string,reason:string)=>request<ExitRequest>('/api/v1/providers/me/exit/requests',body({requestType,reason,idempotencyKey:crypto.randomUUID()}))
export const cancelProviderExit=(id:string,reason:string,rowVersion:string)=>request<ExitRequest>(`/api/v1/providers/me/exit/requests/${id}/cancel`,body({reason,rowVersion}))
export const searchAdminExits=(status:string,page:number)=>request<AdminExitList>(`/api/v1/admin/provider-exits?status=${encodeURIComponent(status)}&page=${page}&pageSize=20`)
export const getAdminExit=(id:string)=>request<AdminExitDetail>(`/api/v1/admin/provider-exits/${id}`)
export const recheckAdminExit=(id:string,reason:string,rowVersion:string)=>request<AdminExitDetail>(`/api/v1/admin/provider-exits/${id}/recheck`,body({reason,rowVersion}))
export const completeAdminExit=(id:string,reason:string,rowVersion:string)=>request<AdminExitDetail>(`/api/v1/admin/provider-exits/${id}/complete`,body({reason,rowVersion}))
export const rejectAdminExit=(id:string,reason:string,rowVersion:string)=>request<AdminExitDetail>(`/api/v1/admin/provider-exits/${id}/reject`,body({reason,rowVersion}))
