import type { CareApplication, CareContract, CareContractDetail, CareDashboard, CareRequest, CareScheduleChange, CareVisit, CareVisitDetail } from './careTypes'

async function call<T>(path:string, options?:RequestInit):Promise<T> {
  const response=await fetch(path,{credentials:'include',...options,headers:options?.body instanceof FormData?options.headers:options?.body?{'Content-Type':'application/json',...options.headers}:options?.headers})
  if(!response.ok){let message='수달 케어 업무를 처리하지 못했습니다.';try{const value=await response.json() as {message?:string};message=value.message??message}catch{/* fallback */}throw new Error(message)}
  return response.json() as Promise<T>
}
const post=(body:unknown):RequestInit=>({method:'POST',body:JSON.stringify(body)})
const key=(prefix:string)=>`${prefix}-${crypto.randomUUID()}`
export const providerCareApi={
  home:()=>call<CareDashboard>('/api/v1/providers/me/care/home'),
  requests:()=>call<CareRequest[]>('/api/v1/providers/me/care/requests/open'),
  apply:(id:string,body:{proposedScopeText:string;proposedMonthlyAmount:number|null;proposedVisitAmount:number|null;availableScheduleText:string})=>call(`/api/v1/providers/me/care/requests/${id}/applications`,post({...body,idempotencyKey:key(`provider-care-apply-${id}`)})),
  applications:()=>call<CareApplication[]>('/api/v1/providers/me/care/applications'),
  withdrawApplication:(item:CareApplication,reason:string)=>call<CareApplication>(`/api/v1/providers/me/care/applications/${item.id}/withdraw`,post({reason,idempotencyKey:key(`provider-care-withdraw-${item.id}`),rowVersion:item.rowVersion})),
  contracts:(status?:string)=>call<CareContract[]>(`/api/v1/providers/me/care/contracts${status?`?status=${encodeURIComponent(status)}`:''}`),
  contract:(id:string)=>call<CareContractDetail>(`/api/v1/providers/me/care/contracts/${id}`),
  terminateContract:(item:CareContract,reason:string)=>call<CareContract>(`/api/v1/providers/me/care/contracts/${item.id}/terminate`,post({reason,idempotencyKey:key(`provider-care-terminate-${item.id}`),rowVersion:item.rowVersion})),
  visits:(filter?:string)=>call<CareVisit[]>(`/api/v1/providers/me/care/visits${filter?`?filter=${encodeURIComponent(filter)}`:''}`),
  visit:(id:string)=>call<CareVisitDetail>(`/api/v1/providers/me/care/visits/${id}`),
  scheduleChanges:()=>call<CareScheduleChange[]>('/api/v1/providers/me/care/schedule-changes'),
  requestSchedule:(id:string,scheduledStartAt:string,scheduledEndAt:string|null,reason:string)=>call(`/api/v1/providers/me/care/visits/${id}/schedule-changes`,post({scheduledStartAt,scheduledEndAt,reason,idempotencyKey:key(`provider-care-schedule-${id}`)})),
  decideSchedule:(item:CareScheduleChange,approve:boolean)=>call(`/api/v1/providers/me/care/schedule-changes/${item.id}/decision`,post({approve,idempotencyKey:key(`provider-care-decision-${item.id}`),rowVersion:item.rowVersion})),
  start:(visit:CareVisit)=>call<CareVisitDetail>(`/api/v1/providers/me/care/visits/${visit.id}/start`,post({idempotencyKey:key(`provider-care-start-${visit.id}`),rowVersion:visit.rowVersion})),
  upload:async(id:string,file:File)=>{const body=new FormData();body.append('file',file);return call<{fileId:string}>(`/api/v1/providers/me/care/visits/${id}/files`,{method:'POST',body})},
  complete:(visit:CareVisit,fileIds:string[],checklistJson:string,note:string)=>call(`/api/v1/providers/me/care/visits/${visit.id}/completion`,post({verificationMethodCode:null,verificationResultCode:null,checklistJson,completionNote:note,fileIds,idempotencyKey:key(`provider-care-complete-${visit.id}`),rowVersion:visit.rowVersion,gpsEvidence:null,possessionEvidence:null})),
}
