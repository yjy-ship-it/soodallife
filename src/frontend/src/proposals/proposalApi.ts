export interface ProposalArea { id:string;name:string;parentName:string|null }
export interface ProposalServiceOption { id:string;name:string;categoryPath:string;isNationwide:boolean;areas:ProposalArea[] }
export interface ProposalSetup { services:ProposalServiceOption[];feePerConfirmedParticipant:number;maximumLocalAreas:number;maximumActiveCampaigns:number;availableWalletBalance:number;walletStatusCode:string }
export interface ProposalCampaign { id:string;proposalTypeCode:string;scopeCode:string;title:string;summary:string;serviceCategoryId:string;serviceName:string;categoryPath:string;providerId:string;providerName:string;normalPriceAmount:number|null;offerPriceAmount:number;minimumParticipants:number;maximumParticipants:number;confirmedParticipants:number;startAt:string;endAt:string;serviceAt:string|null;cancellationPolicyText:string;statusCode:string;feePerParticipant:number;reservedFeeAmount:number;capturedFeeAmount:number;feeStatusCode:string;areas:ProposalArea[];canApply:boolean;myApplicationStatus:string|null }
export interface ProposalApplication { id:string;campaignId:string;customerId:string;customerName:string;statusCode:string;appliedAt:string;confirmedAt:string|null }
export interface CustomerProposalParticipation { applicationId:string;campaignId:string;title:string;providerId:string;providerName:string;serviceCategoryId:string;serviceName:string;applicationStatusCode:string;campaignStatusCode:string;offerPriceAmount:number;appliedAt:string;confirmedAt:string|null;cancelledAt:string|null;declinedAt:string|null;endAt:string;serviceAt:string|null;canCancel:boolean;nextStep:string }
export interface ProposalInterest { categoryIds:string[];areaIds:string[] }
export interface SaveProposal { proposalTypeCode:string;scopeCode:string;serviceCategoryId:string;areaIds:string[];title:string;summary:string;normalPriceAmount:number|null;offerPriceAmount:number;minimumParticipants:number;maximumParticipants:number;startAt:string;endAt:string;serviceAt:string|null;cancellationPolicyText:string;idempotencyKey:string }

async function request<T>(path:string,init?:RequestInit):Promise<T>{const response=await fetch(path,{credentials:'include',headers:init?.body?{'Content-Type':'application/json'}:undefined,...init});if(!response.ok){let message='제안·공동모집을 처리하지 못했습니다.';try{const body=await response.json() as {message?:string};if(body.message)message=body.message}catch{/* 공통 안내 */}throw new Error(message)}return response.status===204?undefined as T:response.json() as Promise<T>}
const provider='/api/v1/providers/me/proposals',customer='/api/v1/customers/me/proposals',publicRoot='/api/v1/public/proposals'
export const proposalApi={
 setup:()=>request<ProposalSetup>(`${provider}/setup`), providerList:()=>request<{items:ProposalCampaign[]}>(provider),
 create:(value:SaveProposal)=>request<ProposalCampaign>(provider,{method:'POST',body:JSON.stringify(value)}),
 update:(id:string,value:SaveProposal)=>request<ProposalCampaign>(`${provider}/${id}`,{method:'PUT',body:JSON.stringify(value)}),
 applications:(id:string)=>request<{items:ProposalApplication[]}>(`${provider}/${id}/applications`),
 confirm:(id:string,applicationId:string)=>request<ProposalApplication>(`${provider}/${id}/applications/${applicationId}/confirm`,{method:'POST'}),
 decline:(id:string,applicationId:string)=>request<ProposalApplication>(`${provider}/${id}/applications/${applicationId}/decline`,{method:'POST'}),
 cancelCampaign:(id:string)=>request<ProposalCampaign>(`${provider}/${id}/cancel`,{method:'POST'}),
 publicList:(take=20,areaId?:string)=>request<{items:ProposalCampaign[]}>(`${publicRoot}?take=${take}${areaId?`&areaId=${areaId}`:''}`),
 detail:(id:string)=>request<ProposalCampaign>(`${publicRoot}/${id}`),
 interests:()=>request<ProposalInterest>(`${customer}/interests`),
 myApplications:()=>request<CustomerProposalParticipation[]>(`${customer}/applications`),
 saveInterests:(value:ProposalInterest)=>request<ProposalInterest>(`${customer}/interests`,{method:'PUT',body:JSON.stringify(value)}),
 recordSignal:(serviceCategoryId:string,signalTypeCode='SERVICE_DETAIL')=>request<void>(`${customer}/signals`,{method:'POST',body:JSON.stringify({serviceCategoryId,signalTypeCode})}),
 apply:(id:string)=>request<ProposalApplication>(`${customer}/${id}/applications`,{method:'POST'}),
 cancelApplication:(id:string)=>request<ProposalApplication>(`${customer}/${id}/applications/me`,{method:'DELETE'}),
}
