export interface AdminTrustList {
  totalCount:number; page:number; pageSize:number; items:AdminTrustListItem[]
}
export interface AdminTrustListItem {
  providerId:string; providerName:string; maskedPhone:string|null; maskedEmail:string|null; maskedBusinessRegistrationNo:string|null
  accountStatusCode:string; approvalStatusCode:string; activityStatusCode:string; score:number|null; gradeLabel:string
  evaluationStatusCode:string; isLegacyUnknownPolicy:boolean; completedTransactionCount:number; afterServiceCount:number
  disputeCount:number; evidenceStatus:string; calculatedAt:string|null
}
export interface AdminTrustDetail {
  provider:{id:string;providerName:string;phone:string|null;email:string|null;businessRegistrationNo:string|null;accountStatusCode:string;approvalStatusCode:string;activityStatusCode:string}
  trust:{score:number|null;gradeLabel:string;evaluationStatusCode:string;statusNotice:string;policyVersion:string|null;calculatedAt:string|null}
  performance:{submittedQuoteCount:number;acceptedQuoteCount:number;transactionCount:number;completedTransactionCount:number;cancelledTransactionCount:number}
  documents:Array<{id:number;documentType:string;verificationStatusCode:string;expiresAt:string|null;isExpired:boolean;verifiedAt:string|null}>
  afterServices:Array<{id:string;subject:string;statusCode:string;receivedAt:string;completedAt:string|null;resolutionSummary:string|null;unresolvedReason:string|null;recurrenceOccurred:boolean|null;convertedToDispute:boolean}>
  disputes:Array<{id:string;subject:string;statusCode:string;receivedAt:string;resolvedAt:string|null;closedAt:string|null;responsibilityNotice:string}>
  events:Array<{id:string;occurredAt:string;eventTypeCode:string;sourceTypeCode:string;sourcePublicId:string|null;scoreBefore:number|null;scoreDelta:number|null;scoreAfter:number|null;gradeBefore:string|null;gradeAfter:string|null;reasonText:string|null;policyVersion:string|null;processedAt:string|null}>
  reviews:{reviewCount:number;publicReviewCount:number;ratingItemAverages:Array<{itemId:string;itemName:string;averageValue:number;ratingCount:number;minValue:number;maxValue:number}>}
  reviewNotice:string
}
