import type {InteriorHome,InteriorProjectDetail,InteriorProjectList,InteriorRequestCandidate,InteriorService} from './interiorTypes'

export class InteriorApiError extends Error{readonly status:number;readonly code?:string;constructor(message:string,status:number,code?:string){super(message);this.status=status;this.code=code}}
async function request<T>(path:string,options?:RequestInit):Promise<T>{const response=await fetch(path,{credentials:'include',...options,headers:options?.body?{'Content-Type':'application/json',...options.headers}:options?.headers});if(!response.ok){let message='인테리어 요청을 처리하지 못했습니다.';let code:string|undefined;try{const body=await response.json() as {message?:string;code?:string;businessCode?:string};message=body.message??message;code=body.code??body.businessCode}catch{/* fallback */}throw new InteriorApiError(message,response.status,code)}return response.status===204?undefined as T:response.json() as Promise<T>}
const post=(body:unknown):RequestInit=>({method:'POST',body:JSON.stringify(body)})
export const interiorApi={
  services:()=>request<InteriorService[]>('/api/v1/public/interior/services'),
  home:()=>request<InteriorHome>('/api/v1/customers/me/interior/home'),
  candidates:()=>request<InteriorRequestCandidate[]>('/api/v1/customers/me/interior/request-candidates'),
  projects:()=>request<InteriorProjectList[]>('/api/v1/customers/me/interior/projects'),
  project:(id:string)=>request<InteriorProjectDetail>(`/api/v1/customers/me/interior/projects/${id}`),
  create:(serviceRequestId:string)=>request<InteriorProjectDetail>('/api/v1/customers/me/interior/projects',post({serviceRequestId,idempotencyKey:`customer-interior-create-${crypto.randomUUID()}`})),
  selectVisit:(projectId:string,visitId:string)=>request<InteriorProjectDetail>(`/api/v1/customers/me/interior/projects/${projectId}/site-visits/${visitId}/selection`,post({idempotencyKey:`customer-interior-visit-${visitId}-${crypto.randomUUID()}`})),
  agree:(projectId:string,contractId:string)=>request<InteriorProjectDetail>(`/api/v1/customers/me/interior/projects/${projectId}/contracts/${contractId}/agreement`,post({idempotencyKey:`customer-interior-agree-${contractId}-${crypto.randomUUID()}`})),
  confirmPayment:(projectId:string,planId:string,amount:number,note:string)=>request<string>(`/api/v1/customers/me/interior/projects/${projectId}/payment-plans/${planId}/confirmations`,post({confirmationTypeCode:'CUSTOMER_DIRECT_PAYMENT',amount,confirmedAt:new Date().toISOString(),evidenceFileId:null,note,idempotencyKey:`customer-interior-payment-${planId}-${crypto.randomUUID()}`})),
  decideChange:(projectId:string,changeId:string,approve:boolean)=>request<InteriorProjectDetail>(`/api/v1/customers/me/interior/projects/${projectId}/changes/${changeId}/decision`,post({approve,idempotencyKey:`customer-interior-change-${changeId}-${crypto.randomUUID()}`})),
  upload:async(projectId:string,file:File)=>{const form=new FormData();form.append('file',file);return request<{id:string;fileName:string;contentType:string;sizeBytes:number;scanStatus:string}>(`/api/v1/customers/me/interior/projects/${projectId}/evidence-files`,{method:'POST',body:form})},
  defect:(projectId:string,body:unknown)=>request<InteriorProjectDetail>(`/api/v1/customers/me/interior/projects/${projectId}/defects`,post(body)),
  dispute:(projectId:string,body:unknown)=>request<InteriorProjectDetail>(`/api/v1/customers/me/interior/projects/${projectId}/disputes`,post(body)),
}
