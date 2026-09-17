using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Features.Proposals;

public sealed partial class ProposalService
{
    public async Task<ProposalCampaignResponse> Create(ClaimsPrincipal principal, SaveProviderProposalRequest input, CancellationToken token)
    {
        Validate(input);
        var identity = await Provider(principal, token);
        var now = DateTime.UtcNow;
        var activeCount = await db.ProviderProposalCampaigns.CountAsync(x => x.ProviderProfileId == identity.ProfileId &&
            (x.StatusCode == "PUBLISHED" || x.StatusCode == "MINIMUM_MET" || x.StatusCode == "FULL"), token);
        if (activeCount >= MaximumActiveCampaigns)
            throw Error("PROPOSAL_ACTIVE_LIMIT", $"동시에 진행할 수 있는 모집은 최대 {MaximumActiveCampaigns}개입니다.", 409);

        var service = await (from ps in db.ProviderServiceCategories
            join category in db.ServiceCategories on ps.CategoryId equals category.Id
            join approval in db.ProviderServiceApprovals on ps.Id equals approval.ProviderServiceCategoryId
            where ps.ProviderProfileId == identity.ProfileId && category.PublicId == input.ServiceCategoryId &&
                  ps.StatusCode == "ACTIVE" && category.StatusCode == "ACTIVE" && category.LevelCode == "SERVICE" &&
                  approval.ApprovalStatusCode == "APPROVED"
            select new { ProviderService = ps, Category = category }).SingleOrDefaultAsync(token)
            ?? throw Error("PROPOSAL_SERVICE_NOT_APPROVED", "승인되어 활동 중인 서비스만 모집할 수 있습니다.", 403);

        var scope = input.ScopeCode.Trim().ToUpperInvariant();
        var requestedAreaIds = (input.AreaIds ?? []).Distinct().ToArray();
        if (scope == "NATIONWIDE")
        {
            if (!service.ProviderService.IsNationwide)
                throw Error("PROPOSAL_NATIONWIDE_NOT_ALLOWED", "전국·원격 서비스로 승인된 서비스만 전국 모집을 할 수 있습니다.");
            if (requestedAreaIds.Length > 0)
                throw Error("PROPOSAL_NATIONWIDE_AREA_NOT_ALLOWED", "전국 모집에는 개별 활동지역을 지정하지 않습니다.");
        }
        else
        {
            if (requestedAreaIds.Length is < 1 or > MaximumLocalAreas)
                throw Error("PROPOSAL_AREA_LIMIT", $"지역 모집은 1개 이상 {MaximumLocalAreas}개 이하 시·군·구를 선택해 주세요.");
        }

        var validAreas = scope == "LOCAL"
            ? await (from link in db.ProviderServiceAreas
                join area in db.AdministrativeAreas on link.AdministrativeAreaId equals area.Id
                where link.ProviderServiceCategoryId == service.ProviderService.Id && link.StatusCode == "ACTIVE" &&
                      area.AreaLevelCode == "SIGUNGU" && requestedAreaIds.Contains(area.PublicId)
                select area).ToListAsync(token)
            : [];
        if (scope == "LOCAL" && validAreas.Count != requestedAreaIds.Length)
            throw Error("PROPOSAL_AREA_NOT_APPROVED", "해당 서비스에 저장된 실제 활동지역만 선택할 수 있습니다.", 403);

        var wallet = await db.ProviderWallets.SingleOrDefaultAsync(x => x.ProviderProfileId == identity.ProfileId && x.StatusCode == "ACTIVE", token)
            ?? throw Error("PROPOSAL_WALLET_NOT_FOUND", "활성 충전금 지갑을 확인할 수 없습니다.", 409);
        var reservedAmount = FeePerPerson * input.MaximumParticipants;
        if (wallet.AvailableBalance < reservedAmount)
            throw Error("PROPOSAL_WALLET_INSUFFICIENT", $"모집 등록에는 {reservedAmount:N0}원의 충전금이 필요합니다. 먼저 충전해 주세요.", 409);

        var campaign = new ProviderProposalCampaign
        {
            ProviderProfileId = identity.ProfileId, ProviderServiceCategoryId = service.ProviderService.Id, WalletId = wallet.Id,
            ProposalTypeCode = input.ProposalTypeCode.Trim().ToUpperInvariant(), ScopeCode = scope,
            Title = input.Title.Trim(), Summary = input.Summary.Trim(), NormalPriceAmount = input.NormalPriceAmount,
            OfferPriceAmount = input.OfferPriceAmount, MinimumParticipants = input.MinimumParticipants,
            MaximumParticipants = input.MaximumParticipants, StartAt = input.StartAt.ToUniversalTime(), EndAt = input.EndAt.ToUniversalTime(),
            ServiceAt = input.ServiceAt?.ToUniversalTime(), CancellationPolicyText = input.CancellationPolicyText.Trim(),
            FeePerParticipant = FeePerPerson, ReservedFeeAmount = reservedAmount, FeeStatusCode = "RESERVED",
            StatusCode = "PUBLISHED", CreatedAt = now, CreatedByUserId = identity.UserId, UpdatedAt = now, UpdatedByUserId = identity.UserId
        };

        await using var transaction = await wallets.BeginTransactionAsync(token);
        wallet.AvailableBalance -= reservedAmount;
        wallet.ReservedBalance += reservedAmount;
        wallet.UpdatedAt = now;
        wallet.UpdatedByUserId = identity.UserId;
        var reserve = wallets.AddLedger(wallet, null, "RESERVE", -reservedAmount,
            $"proposal-reserve:{campaign.PublicId:N}", "전문가 제안·공동모집 정원 기준 수수료 예약",
            "PROVIDER_PROPOSAL_CAMPAIGN", campaign.PublicId, null, now, identity.UserId);
        await wallets.SaveWithConcurrencyAsync(token);
        campaign.ReserveLedgerEntryId = reserve.Id;
        db.ProviderProposalCampaigns.Add(campaign);
        await wallets.SaveWithConcurrencyAsync(token);
        foreach (var area in validAreas)
            db.ProviderProposalAreas.Add(new ProviderProposalArea { CampaignId = campaign.Id, AdministrativeAreaId = area.Id, CreatedAt = now, CreatedByUserId = identity.UserId });
        await wallets.SaveWithConcurrencyAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return await Build(campaign.Id, null, token);
    }

    public async Task<ProposalApplicationResponse> Confirm(ClaimsPrincipal principal, Guid campaignId, Guid applicationId, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        await using var transaction = await wallets.BeginTransactionAsync(token);
        var campaign = await db.ProviderProposalCampaigns.SingleOrDefaultAsync(x => x.PublicId == campaignId && x.ProviderProfileId == identity.ProfileId, token)
            ?? throw Error("PROPOSAL_NOT_FOUND", "모집을 찾을 수 없습니다.", 404);
        var application = await db.ProviderProposalApplications.SingleOrDefaultAsync(x => x.PublicId == applicationId && x.CampaignId == campaign.Id, token)
            ?? throw Error("PROPOSAL_APPLICATION_NOT_FOUND", "참여 신청을 찾을 수 없습니다.", 404);
        if (application.StatusCode != "APPLIED") throw Error("PROPOSAL_APPLICATION_ALREADY_PROCESSED", "이미 처리된 참여 신청입니다.", 409);
        if (campaign.StatusCode is not ("PUBLISHED" or "MINIMUM_MET") || campaign.EndAt <= DateTime.UtcNow || campaign.ConfirmedParticipants >= campaign.MaximumParticipants)
            throw Error("PROPOSAL_CONFIRMATION_CLOSED", "모집이 마감되어 참여를 확정할 수 없습니다.", 409);

        var wallet = await db.ProviderWallets.SingleAsync(x => x.Id == campaign.WalletId, token);
        if (wallet.ReservedBalance < FeePerPerson || campaign.ReservedFeeAmount < FeePerPerson)
            throw Error("PROPOSAL_RESERVED_FEE_INVALID", "예약된 모집 수수료를 확인할 수 없습니다.", 409);
        var now = DateTime.UtcNow;
        wallet.ReservedBalance -= FeePerPerson;
        wallet.AvailableBalance += FeePerPerson;
        wallets.AddLedger(wallet, null, "RELEASE", FeePerPerson, $"proposal-capture-release:{application.PublicId:N}",
            "참여 확정 수수료 예약 전환", "PROVIDER_PROPOSAL_CAMPAIGN", campaign.PublicId, null, now, identity.UserId);
        wallet.AvailableBalance -= FeePerPerson;
        var capture = wallets.AddLedger(wallet, null, "USE", -FeePerPerson, $"proposal-capture:{application.PublicId:N}",
            "제안·공동모집 참여 확정 수수료", "PROVIDER_PROPOSAL_CAMPAIGN", campaign.PublicId, null, now, identity.UserId);
        wallet.UpdatedAt = now; wallet.UpdatedByUserId = identity.UserId;
        application.StatusCode = "CONFIRMED"; application.ConfirmedAt = now; application.CapturedFeeAmount = FeePerPerson;
        application.CaptureLedgerEntryId = capture.Id; application.UpdatedAt = now; application.UpdatedByUserId = identity.UserId;
        campaign.ConfirmedParticipants++; campaign.ReservedFeeAmount -= FeePerPerson; campaign.CapturedFeeAmount += FeePerPerson;
        campaign.StatusCode = campaign.ConfirmedParticipants >= campaign.MaximumParticipants ? "FULL" :
            campaign.ConfirmedParticipants >= campaign.MinimumParticipants ? "MINIMUM_MET" : "PUBLISHED";
        campaign.FeeStatusCode = campaign.ReservedFeeAmount == 0 ? "CAPTURED" : "PARTIALLY_CAPTURED";
        campaign.UpdatedAt = now; campaign.UpdatedByUserId = identity.UserId;
        await wallets.SaveWithConcurrencyAsync(token);
        await AddBusinessNotification(application.CustomerProfileId, campaign, "참여가 확정되었습니다", "전문가가 공동모집 참여를 확정했습니다.", $"confirmed:{application.PublicId:N}", token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return await Application(application.Id, campaign.PublicId, token);
    }

    public async Task<ProposalApplicationResponse> Decline(ClaimsPrincipal principal, Guid campaignId, Guid applicationId, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        var campaign = await db.ProviderProposalCampaigns.SingleOrDefaultAsync(x => x.PublicId == campaignId && x.ProviderProfileId == identity.ProfileId, token)
            ?? throw Error("PROPOSAL_NOT_FOUND", "모집을 찾을 수 없습니다.", 404);
        var application = await db.ProviderProposalApplications.SingleOrDefaultAsync(x => x.PublicId == applicationId && x.CampaignId == campaign.Id, token)
            ?? throw Error("PROPOSAL_APPLICATION_NOT_FOUND", "참여 신청을 찾을 수 없습니다.", 404);
        if (application.StatusCode != "APPLIED") throw Error("PROPOSAL_APPLICATION_ALREADY_PROCESSED", "이미 처리된 참여 신청입니다.", 409);
        var now = DateTime.UtcNow; application.StatusCode = "DECLINED"; application.DeclinedAt = now; application.UpdatedAt = now; application.UpdatedByUserId = identity.UserId;
        await db.SaveChangesAsync(token);
        await AddBusinessNotification(application.CustomerProfileId, campaign, "참여 신청 결과 안내", "이번 공동모집 참여가 확정되지 않았습니다.", $"declined:{application.PublicId:N}", token);
        return await Application(application.Id, campaign.PublicId, token);
    }

    public async Task<ProposalCampaignResponse> CancelCampaign(ClaimsPrincipal principal, Guid campaignId, CancellationToken token)
    {
        var identity = await Provider(principal, token);
        await using var transaction = await wallets.BeginTransactionAsync(token);
        var campaign = await db.ProviderProposalCampaigns.SingleOrDefaultAsync(x => x.PublicId == campaignId && x.ProviderProfileId == identity.ProfileId, token)
            ?? throw Error("PROPOSAL_NOT_FOUND", "모집을 찾을 수 없습니다.", 404);
        if (campaign.StatusCode is "CANCELLED" or "EXPIRED") throw Error("PROPOSAL_ALREADY_CLOSED", "이미 종료된 모집입니다.", 409);
        await RestoreAndRelease(campaign, identity.UserId, "전문가 모집 취소", true, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return await Build(campaign.Id, null, token);
    }

    private async Task RestoreAndRelease(ProviderProposalCampaign campaign, long actor, string reason, bool cancelApplications, CancellationToken token)
    {
        var wallet = await db.ProviderWallets.SingleAsync(x => x.Id == campaign.WalletId, token);
        var now = DateTime.UtcNow;
        if (campaign.ReservedFeeAmount > 0)
        {
            wallet.ReservedBalance -= campaign.ReservedFeeAmount; wallet.AvailableBalance += campaign.ReservedFeeAmount;
            var release = wallets.AddLedger(wallet, null, "RELEASE", campaign.ReservedFeeAmount, $"proposal-release:{campaign.PublicId:N}:{now.Ticks}", reason,
                "PROVIDER_PROPOSAL_CAMPAIGN", campaign.PublicId, null, now, actor);
            campaign.ReleaseLedgerEntryId = release.Id; campaign.ReservedFeeAmount = 0;
        }
        if (campaign.CapturedFeeAmount > 0)
        {
            wallet.AvailableBalance += campaign.CapturedFeeAmount;
            wallets.AddLedger(wallet, null, "RESTORE", campaign.CapturedFeeAmount, $"proposal-restore:{campaign.PublicId:N}:{now.Ticks}", reason,
                "PROVIDER_PROPOSAL_CAMPAIGN", campaign.PublicId, null, now, actor);
            campaign.CapturedFeeAmount = 0;
        }
        if (cancelApplications)
        {
            var applications = await db.ProviderProposalApplications.Where(x => x.CampaignId == campaign.Id && (x.StatusCode == "APPLIED" || x.StatusCode == "CONFIRMED")).ToListAsync(token);
            foreach (var application in applications)
            {
                application.StatusCode = "CANCELLED"; application.CancelledAt = now; application.UpdatedAt = now; application.UpdatedByUserId = actor;
                await AddBusinessNotification(application.CustomerProfileId, campaign, "공동모집 취소 안내", reason, $"cancelled:{application.PublicId:N}", token);
            }
        }
        campaign.StatusCode = "CANCELLED"; campaign.FeeStatusCode = "RESTORED"; campaign.CancelledAt = now; campaign.ClosedAt = now; campaign.UpdatedAt = now; campaign.UpdatedByUserId = actor;
        wallet.UpdatedAt = now; wallet.UpdatedByUserId = actor;
        await wallets.SaveWithConcurrencyAsync(token);
    }
}
