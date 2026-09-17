using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Proposals;

public sealed class ProposalOptions
{
    public const string SectionName = "ProviderProposals";
    public bool MarketingTermsFinalized { get; set; }
    public bool PwaPushEnabled { get; set; }
}

public sealed record SaveProviderProposalRequest(
    [param:Required,StringLength(30)] string ProposalTypeCode,
    [param:Required,StringLength(20)] string ScopeCode,
    Guid ServiceCategoryId,
    IReadOnlyList<Guid>? AreaIds,
    [param:Required,StringLength(200)] string Title,
    [param:Required,StringLength(3000)] string Summary,
    decimal? NormalPriceAmount,
    decimal OfferPriceAmount,
    int MinimumParticipants,
    int MaximumParticipants,
    DateTime StartAt,
    DateTime EndAt,
    DateTime? ServiceAt,
    [param:Required,StringLength(2000)] string CancellationPolicyText,
    string? IdempotencyKey = null);

public sealed record ProposalAreaResponse(Guid Id,string Name,string? ParentName);
public sealed record ProposalServiceOptionResponse(Guid Id,string Name,string CategoryPath,bool IsNationwide,IReadOnlyList<ProposalAreaResponse> Areas);
public sealed record ProviderProposalSetupResponse(IReadOnlyList<ProposalServiceOptionResponse> Services,decimal FeePerConfirmedParticipant,int MaximumLocalAreas,int MaximumActiveCampaigns,decimal AvailableWalletBalance,string WalletStatusCode);
public sealed record ProposalCampaignResponse(Guid Id,string ProposalTypeCode,string ScopeCode,string Title,string Summary,Guid ServiceCategoryId,string ServiceName,string CategoryPath,Guid ProviderId,string ProviderName,decimal? NormalPriceAmount,decimal OfferPriceAmount,int MinimumParticipants,int MaximumParticipants,int ConfirmedParticipants,DateTime StartAt,DateTime EndAt,DateTime? ServiceAt,string CancellationPolicyText,string StatusCode,decimal FeePerParticipant,decimal ReservedFeeAmount,decimal CapturedFeeAmount,string FeeStatusCode,IReadOnlyList<ProposalAreaResponse> Areas,bool CanApply,string? MyApplicationStatus);
public sealed record ProposalCampaignListResponse(IReadOnlyList<ProposalCampaignResponse> Items);
public sealed record ProposalApplicationResponse(Guid Id,Guid CampaignId,Guid CustomerId,string CustomerName,string StatusCode,DateTime AppliedAt,DateTime? ConfirmedAt);
public sealed record ProposalApplicationListResponse(IReadOnlyList<ProposalApplicationResponse> Items);
public sealed record CustomerProposalParticipationResponse(Guid ApplicationId,Guid CampaignId,string Title,Guid ProviderId,string ProviderName,Guid ServiceCategoryId,string ServiceName,string ApplicationStatusCode,string CampaignStatusCode,decimal OfferPriceAmount,DateTime AppliedAt,DateTime? ConfirmedAt,DateTime? CancelledAt,DateTime? DeclinedAt,DateTime EndAt,DateTime? ServiceAt,bool CanCancel,string NextStep);
public sealed record ProposalInterestResponse(IReadOnlyList<Guid> CategoryIds,IReadOnlyList<Guid> AreaIds);
public sealed record SaveProposalInterestRequest(IReadOnlyList<Guid>? CategoryIds,IReadOnlyList<Guid>? AreaIds);
public sealed record InterestedServiceResponse(Guid Id,string? Code,string? Slug,string Name,string CategoryPath,bool SubscriptionAvailable,DateTime SavedAt);
public sealed record InterestedServiceStateResponse(Guid ServiceId,bool Interested);
public sealed record SaveInterestedServiceRequest(bool Interested);
public sealed record RecordProposalSignalRequest(Guid ServiceCategoryId,string SignalTypeCode);
public sealed record ProposalPolicyGuideResponse(string BillingModel,string NationwideChannels,string LocalChannels,string ExternalDeliveryGate,IReadOnlyList<string> Rules);

public sealed class ProposalException(string businessCode,string message,int statusCode=400):Exception(message)
{ public string BusinessCode{get;}=businessCode;public int StatusCode{get;}=statusCode; }
