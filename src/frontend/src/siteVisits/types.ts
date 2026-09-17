export interface SiteVisitEvent { id:string; eventType:string; note:string|null; occurredAt:string }
export interface SiteVisitProposal {
  id:string; requestId:string; providerId:string; providerName:string; status:string; scheduledAt:string;
  estimatedDurationMinutes:number; visitFeeAmount:number; paymentMode:string; paymentStatus:string;
  deductFromWorkAmount:boolean; terms:string|null; paymentInstruction:string|null; noShowWaitMinutes:number;
  expiresAt:string; acceptedAt:string|null; departedAt:string|null; arrivedAt:string|null; completedAt:string|null;
  noShowStatus:string|null; detailAddress:string|null; canAccept:boolean; canEdit:boolean; canProgress:boolean;
  events:SiteVisitEvent[]; rowVersion:string;
}
export interface SaveSiteVisitProposal {
  scheduledAt:string; estimatedDurationMinutes:number; visitFeeAmount:number; paymentMode:string; terms:string|null;
  deductFromWorkAmount:boolean; paymentInstruction:string|null; noShowWaitMinutes:number; expiresAt:string;
  idempotencyKey:string; rowVersion:string|null;
}
