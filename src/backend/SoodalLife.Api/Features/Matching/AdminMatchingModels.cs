namespace SoodalLife.Api.Features.Matching;

public sealed record AdminMatchingSummary(int OpenRequestCount,int UnmatchedRequestCount,int ActiveDispatchCount,int CoveredAreaCount,int EligibleProviderCount);
public sealed record AdminMatchingRegion(Guid AreaId,string ProvinceName,string DistrictName,int ActiveProviderCount,int ActiveServiceCount,int OpenRequestCount,int UnmatchedRequestCount);
public sealed record AdminMatchingRequest(Guid RequestId,Guid AreaId,string Title,string CategoryName,string ProvinceName,string DistrictName,string Status,bool IsUrgent,DateTime? OpenedAt,DateTime? ExpiresAt,int CandidateCount,int EligibleCandidateCount,int DispatchCount,int QuoteCount);
public sealed record AdminMatchingDashboard(AdminMatchingSummary Summary,IReadOnlyList<AdminMatchingRegion> Regions,IReadOnlyList<AdminMatchingRequest> Requests);
