export interface ProviderProfile {
  id: string
  businessName: string
  representativeName: string | null
  contactName: string | null
  phone: string | null
  email: string | null
  businessRegistrationNumber: string | null
  businessAddress: string | null
  businessTypeText: string | null
  businessItemText: string | null
  introduction: string | null
  providerType: string | null
  approvalStatus: string
  activityStatus: string
  trustScore: number | null
  trustDisplayStatus: string
  rejectionReason: string | null
  concurrencyToken: string
}

export interface ProviderServiceCategory {
  categoryId: string
  categoryPath: string
  status: string
  approvalStatus: string
  decisionReason: string | null
  requiredRequirementCount: number
  approvedRequirementCount: number
}

export interface ProviderEvidenceType { documentTypeId: string; code: string; name: string; isRequired: boolean }
export interface ProviderRequirement { verificationId: string; assignmentId: string; serviceCategoryId: string; categoryPath: string; requirementCode: string; requirementName: string; requirementType: string; isRequired: boolean; verificationRequired: boolean; expiryCheckRequired: boolean; minimumValidDays: number | null; verificationStatus: string; documentId: string | null; documentName: string | null; expiresAt: string | null; rejectionReason: string | null; acceptedEvidenceTypes: ProviderEvidenceType[] }
export interface ProviderDocumentType { id: string; code: string; name: string; description: string | null }
export interface ProviderDocument { id: string; fileId: string; documentTypeCode: string; documentTypeName: string; originalFileName: string; sizeBytes: number; contentType: string; verificationStatus: string; malwareStatus: string; issuedAt: string | null; expiresAt: string | null; publicNote: string | null; createdAt: string }
export interface ProviderDashboard { approvalStatus: string; activityStatus: string; registeredServiceCount: number; approvedServiceCount: number; pendingServiceCount: number; rejectedServiceCount: number; activeAreaCount: number; requiredEvidenceCount: number; submittedEvidenceCount: number; approvedEvidenceCount: number; nextActions: string[]; rejectionReason: string | null }
export interface ProviderOperationsDashboard { newMatchedRequestCount:number; submittedQuoteCount:number; waitingSelectionQuoteCount:number; selectedTransactionCount:number; appointmentActionRequiredCount:number; todayAppointmentCount:number; inProgressWorkCount:number; waitingCompletionConfirmationCount:number; revisionRequestedCount:number; unreadNotificationCount:number }
export interface ProviderHubMetric { key:string; label:string; count:number; route:string; tone:string }
export interface ProviderHubMetricGroup { key:string; title:string; items:ProviderHubMetric[] }
export interface ProviderHubWorkItem { type:string; publicId:string; title:string; description:string; status:string; priorityGroup:string; scheduledAt:string|null; badge:string; route:string; nextAction:string; domain:string }
export interface ProviderHubInboxPage { items:ProviderHubWorkItem[]; page:number; pageSize:number; totalCount:number; totalPages:number }
export interface ProviderHubScheduleItem { type:string; publicId:string; title:string; status:string; scheduledAt:string; scheduledEndAt:string|null; route:string; domain:string }
export interface ProviderHubChatItem { roomId:string; counterpartyDisplayName:string; serviceName:string; lastMessageAt:string|null; unreadCount:number; route:string }
export interface ProviderHubEmergency { isEnabled:boolean; isCurrentlyAvailable:boolean; availabilityReason:string; newRequestCount:number; waitingResponseCount:number; activeAssignmentCount:number; todayAvailability:string; route:string }
export interface ProviderHubWallet { availableBalance:number; reservedBalance:number; currencyCode:string; status:string; latestEntryType:string|null; latestEntryAmount:number|null; latestEntryAt:string|null; requiresAttention:boolean; route:string }
export interface ProviderHubApproval { approvalStatus:string; activityStatus:string; pendingServiceCount:number; rejectedServiceCount:number; missingEvidenceCount:number; rejectedEvidenceCount:number; expiredEvidenceCount:number; nextActions:string[]; route:string }
export interface ProviderOperationsHub { generatedAt:string; summary:ProviderHubMetricGroup[]; inbox:ProviderHubInboxPage; schedule:ProviderHubScheduleItem[]; recentChats:ProviderHubChatItem[]; emergency:ProviderHubEmergency; wallet:ProviderHubWallet; approval:ProviderHubApproval }
export interface ProviderLegalDocument { id: string; versionId: string; code: string; requirementCode: string; title: string; content: string; version: number; effectiveFrom: string; effectiveTo: string | null; isPlaceholder: boolean }

export interface ProviderArea {
  id: string
  name: string
  areaCode: string
}

export interface ProviderServiceArea {
  serviceCategoryId: string
  categoryPath: string
  areas: ProviderArea[]
}

export interface MatchedRequestListItem {
  requestId: string
  categoryPath: string
  areaName: string
  summary: string
  desiredAt: string | null
  dispatchedAt: string
  dispatchStatus: string
  requestStatus: string
}

export interface MatchedRequestAnswer {
  fieldId: string
  label: string
  inputType: string
  value: unknown
  isMasked: boolean
}
export interface MatchedRequestFile { id:string; fileName:string; contentType:string; sizeBytes:number; malwareScanStatus:string; privacyInspectionStatus:string; sanitizationStatus:string; publicationMode:string; downloadUrl:string }

export interface MatchedRequestDetail extends MatchedRequestListItem {
  title: string
  description: string | null
  isUrgent: boolean
  expiresAt: string
  customerPhone: string | null
  detailAddress: string | null
  answers: MatchedRequestAnswer[]
  files: MatchedRequestFile[]
}

export interface ProviderCaseSource { transactionId:string|null; subscriptionVisitId:string|null; interiorProjectId:string|null; afterServiceId:string|null }
export interface ProviderCaseFile { id:string; fileName:string; contentType:string; sizeBytes:number; role:string|null; description:string|null; sourceType:string; publicationMode:string; downloadUrl:string }
export interface ProviderAfterServiceTimeline { actionType:string; status:string; displayStatus:string; note:string|null; scheduledAt:string|null; performedAt:string|null; occurredAt:string }
export interface ProviderAfterServiceListItem { id:string; caseNumber:string; subject:string; status:string; displayStatus:string; sourceType:string; receivedAt:string; scheduledAt:string|null; contactAvailable:boolean }
export interface ProviderAfterServiceDetail extends ProviderAfterServiceListItem { source:ProviderCaseSource; description:string; requestDetails:string|null; warrantyStartDate:string|null; warrantyEndDate:string|null; isWithinWarranty:boolean|null; dueAt:string|null; providerConfirmedAt:string|null; providerResponse:string|null; visitRequired:boolean|null; startedAt:string|null; completedAt:string|null; resolutionSummary:string|null; unresolvedReason:string|null; recurrenceOccurred:boolean|null; customerName:string; customerPhone:string|null; detailAddress:string|null; contactPolicy:string; rowVersion:string; timeline:ProviderAfterServiceTimeline[]; evidence:ProviderCaseFile[] }
export interface ProviderDisputeTimeline { actionType:string; note:string|null; reason:string|null; occurredAt:string; isProviderSubmission:boolean }
export interface ProviderDisputeListItem { id:string; caseNumber:string; subject:string; status:string; displayStatus:string; sourceType:string; receivedAt:string; lastActionAt:string|null }
export interface ProviderDisputeDetail extends ProviderDisputeListItem { source:ProviderCaseSource; customerClaim:string; dueAt:string|null; resolvedAt:string|null; rowVersion:string; timeline:ProviderDisputeTimeline[]; evidence:ProviderCaseFile[]; providerMayRespond:boolean; decisionPolicy:string }
