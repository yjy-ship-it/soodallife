using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class AdminProviderService(SoodalLifeDbContext dbContext, RequestMatchingService matchingService,
    IWebHostEnvironment environment, IConfiguration configuration, IPrivateFileStorage fileStorage)
{
    public async Task<AdminProviderListResponse> SearchAsync(string? search, string? accountStatus, string? approvalStatus, string? activityStatus,
        string? providerType, Guid? serviceId, Guid? areaId, string? documentStatus, DateOnly? joinedFrom, DateOnly? joinedTo, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new AdminServiceCategoryException("ADMIN_PROVIDER_PAGE_INVALID", "페이지와 페이지당 전문가 수를 확인해 주세요.");
        var query = from provider in dbContext.ProviderProfiles.AsNoTracking()
                    join user in dbContext.Users.AsNoTracking() on provider.UserId equals user.Id
                    where dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RevokedAt == null &&
                        dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Code == "PROVIDER"))
                    select new { Provider = provider, User = user };
        var term = search?.Trim();
        var supplementedSearch = term is "보완" or "보완 제출";
        if (supplementedSearch)
        {
            query = query.Where(row => row.Provider.ApprovalStatusCode == "PENDING" &&
                dbContext.ProviderApprovalEvents.Any(x => x.ProviderProfileId == row.Provider.Id && x.ActionCode == "RESUBMIT"));
            term = null;
        }
        if (!string.IsNullOrWhiteSpace(term))
        {
            if (Guid.TryParse(term, out var id)) query = query.Where(row => row.Provider.PublicId == id || row.User.PublicId == id);
            else query = query.Where(row => row.Provider.BusinessName.Contains(term) || (row.Provider.BusinessRegistrationNo != null && row.Provider.BusinessRegistrationNo.Contains(term)) ||
                (row.User.Phone != null && row.User.Phone.Contains(term)) || (row.User.Email != null && row.User.Email.Contains(term)) ||
                (row.Provider.PublicEmail != null && row.Provider.PublicEmail.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(accountStatus)) query = query.Where(row => row.User.StatusCode == accountStatus.ToUpper());
        if (!string.IsNullOrWhiteSpace(approvalStatus))
        {
            var normalizedApproval = approvalStatus.ToUpperInvariant();
            if (normalizedApproval == "SUPPLEMENTED") query = query.Where(row => row.Provider.ApprovalStatusCode == "PENDING" &&
                dbContext.ProviderApprovalEvents.Any(x => x.ProviderProfileId == row.Provider.Id && x.ActionCode == "RESUBMIT"));
            else query = query.Where(row => row.Provider.ApprovalStatusCode == normalizedApproval);
        }
        if (!string.IsNullOrWhiteSpace(activityStatus)) query = query.Where(row => row.Provider.ActivityStatusCode == activityStatus.ToUpper());
        if (!string.IsNullOrWhiteSpace(providerType)) query = query.Where(row => row.Provider.ProviderTypeCode == providerType.ToUpper());
        if (joinedFrom.HasValue) query = query.Where(row => row.User.CreatedAt >= joinedFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (joinedTo.HasValue) query = query.Where(row => row.User.CreatedAt < joinedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (serviceId.HasValue) query = query.Where(row => dbContext.ProviderServiceCategories.Any(link => link.ProviderProfileId == row.Provider.Id && dbContext.ServiceCategories.Any(category => category.Id == link.CategoryId && category.PublicId == serviceId)));
        if (areaId.HasValue) query = query.Where(row => dbContext.ProviderServiceCategories.Any(link => link.ProviderProfileId == row.Provider.Id && dbContext.ProviderServiceAreas.Any(area => area.ProviderServiceCategoryId == link.Id && dbContext.AdministrativeAreas.Any(value => value.Id == area.AdministrativeAreaId && value.PublicId == areaId))));
        if (!string.IsNullOrWhiteSpace(documentStatus)) query = query.Where(row => dbContext.ProviderDocuments.Any(document => document.ProviderProfileId == row.Provider.Id && document.VerificationStatusCode == documentStatus.ToUpper()));
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(row => row.User.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(row => new { row.Provider, row.User,
                ServiceCount = dbContext.ProviderServiceCategories.Count(link => link.ProviderProfileId == row.Provider.Id),
                DocumentCount = dbContext.ProviderDocuments.Count(document => document.ProviderProfileId == row.Provider.Id),
                LastQuote = dbContext.Quotes.Where(quote => quote.ProviderProfileId == row.Provider.Id).Select(quote => (DateTime?)quote.UpdatedAt).Max(),
                LastTransaction = dbContext.Transactions.Where(transaction => transaction.ProviderProfileId == row.Provider.Id).Select(transaction => (DateTime?)transaction.UpdatedAt).Max(),
                LatestApprovalAction = dbContext.ProviderApprovalEvents.Where(x => x.ProviderProfileId == row.Provider.Id).OrderByDescending(x => x.DecidedAt).Select(x => x.ActionCode).FirstOrDefault() }).ToListAsync(cancellationToken);
        var userIds = rows.Select(row => row.User.Id).ToArray();
        var roles = await (from userRole in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                           where userIds.Contains(userRole.UserId) && userRole.RevokedAt == null select new { userRole.UserId, role.Code }).ToListAsync(cancellationToken);
        return new(total, page, pageSize, rows.Select(row => new AdminProviderListItemResponse(row.Provider.PublicId, row.Provider.BusinessName, row.Provider.BusinessName,
            row.Provider.ProviderTypeCode, MaskPhone(row.User.Phone), MaskEmail(row.User.Email ?? row.Provider.PublicEmail), MaskBusinessNo(row.Provider.BusinessRegistrationNo), row.User.StatusCode,
            row.Provider.ApprovalStatusCode == "PENDING" && row.LatestApprovalAction == "RESUBMIT" ? "SUPPLEMENTED" : row.Provider.ApprovalStatusCode,
            row.Provider.ApprovalStatusCode == "PENDING" && row.LatestApprovalAction == "RESUBMIT" ? "SUPPLEMENTED" : row.Provider.ApprovalStatusCode, row.Provider.ActivityStatusCode, row.ServiceCount, null,
            row.DocumentCount == 0 ? "제출 증빙 없음" : $"제출 {row.DocumentCount}건 · 검증 기준 미확정", row.User.CreatedAt,
            Max(row.LastQuote, row.LastTransaction), roles.Where(role => role.UserId == row.User.Id).Select(role => role.Code).OrderBy(RoleOrder).ToArray())).ToArray());
    }

    public async Task<AdminProviderDetailResponse?> GetAsync(Guid providerPublicId, CancellationToken cancellationToken)
    {
        var identity = await (from provider in dbContext.ProviderProfiles.AsNoTracking() join user in dbContext.Users.AsNoTracking() on provider.UserId equals user.Id
                              where provider.PublicId == providerPublicId && dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RevokedAt == null && dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Code == "PROVIDER"))
                              select new { Provider = provider, User = user }).SingleOrDefaultAsync(cancellationToken);
        if (identity is null) return null;
        var roleRows = await (from userRole in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                              where userRole.UserId == identity.User.Id && userRole.RevokedAt == null select role.Code).ToListAsync(cancellationToken);
        var roles = roleRows.OrderBy(RoleOrder).ToArray();
        var links = await dbContext.ProviderServiceCategories.AsNoTracking().Where(link => link.ProviderProfileId == identity.Provider.Id).OrderBy(link => link.CreatedAt).ToListAsync(cancellationToken);
        var categories = await dbContext.ServiceCategories.AsNoTracking().ToListAsync(cancellationToken);
        var services = links.Select(link => { var service = categories.Single(category => category.Id == link.CategoryId); var middle = categories.SingleOrDefault(category => category.Id == service.ParentId); var major = middle is null ? null : categories.SingleOrDefault(category => category.Id == middle.ParentId);
            return new AdminProviderServiceResponse(link.Id, service.PublicId, major?.Name ?? "대분류 없음", middle?.Name ?? "중분류 없음", service.Name, link.StatusCode, "서비스별 승인상태 구조 없음", link.ActivatedAt, link.DeactivatedAt); }).ToArray();
        var areas = await (from link in dbContext.ProviderServiceCategories.AsNoTracking() join areaLink in dbContext.ProviderServiceAreas.AsNoTracking() on link.Id equals areaLink.ProviderServiceCategoryId
                           join area in dbContext.AdministrativeAreas.AsNoTracking() on areaLink.AdministrativeAreaId equals area.Id
                           join parentArea in dbContext.AdministrativeAreas.AsNoTracking() on area.ParentAreaId equals parentArea.Id into parentAreas from parentArea in parentAreas.DefaultIfEmpty()
                           join category in dbContext.ServiceCategories.AsNoTracking() on link.CategoryId equals category.Id
                           where link.ProviderProfileId == identity.Provider.Id select new AdminProviderAreaResponse(category.PublicId, category.Name, area.PublicId,
                               area.AreaLevelCode == "SIDO" ? area.AreaName : parentArea != null ? parentArea.AreaName : "시·도 미확인",
                               area.AreaLevelCode == "SIGUNGU" ? area.AreaName : "-", area.AreaLevelCode, areaLink.StatusCode, areaLink.ActivatedAt, areaLink.DeactivatedAt, false)).ToListAsync(cancellationToken);
        var documents = await (from document in dbContext.ProviderDocuments.AsNoTracking() join file in dbContext.Files.AsNoTracking() on document.FileId equals file.Id
                               join typeValue in dbContext.ProviderDocumentTypes.AsNoTracking() on document.DocumentTypeId equals typeValue.Id into types from type in types.DefaultIfEmpty()
                               join verifierValue in dbContext.Users.AsNoTracking() on document.VerifiedByUserId equals verifierValue.Id into verifiers from verifier in verifiers.DefaultIfEmpty()
                                where document.ProviderProfileId == identity.Provider.Id select new AdminProviderDocumentResponse(document.Id, file.PublicId, type != null ? type.Name : document.DocumentTypeCode,
                                   document.DocumentTypeCode, AdminPrivacy.DocumentNumber(document.DocumentNumber), document.IssuedAt, document.ExpiresAt, document.VerificationStatusCode, document.VerifiedAt,
                                    verifier != null ? verifier.LoginId : null, file.OriginalFileName, file.StatusCode == "ACTIVE", file.StatusCode == "ACTIVE" ? "열기" : "파일 검사 중")).ToListAsync(cancellationToken);
        var reviews = new List<AdminProviderServiceReviewResponse>();
        foreach (var service in services)
        {
            var categoryId = categories.Single(category => category.PublicId == service.ServiceId).Id;
            var policy = await dbContext.CategoryOperationPolicies.AsNoTracking().Where(item => item.CategoryId == categoryId && item.IsActive).OrderByDescending(item => item.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
            var policyId = policy?.Id ?? 0;
            var assignments = await (from assignment in dbContext.CategoryProviderRequirementAssignments.AsNoTracking() join definition in dbContext.ProviderRequirementDefinitions.AsNoTracking() on assignment.RequirementDefinitionId equals definition.Id
                                     where assignment.CategoryOperationPolicyId == policyId orderby assignment.DisplayOrder select new { Assignment = assignment, Definition = definition }).ToListAsync(cancellationToken);
            var comparisons = new List<AdminProviderRequirementComparisonResponse>();
            foreach (var item in assignments)
            {
                var evidenceNames = await (from link in dbContext.CategoryProviderRequirementEvidenceTypes.AsNoTracking() join type in dbContext.ProviderDocumentTypes.AsNoTracking() on link.DocumentTypeId equals type.Id where link.RequirementAssignmentId == item.Assignment.Id orderby link.DisplayOrder select type.Name).ToListAsync(cancellationToken);
                var verification = await dbContext.ProviderServiceRequirementVerifications.AsNoTracking().SingleOrDefaultAsync(value => value.ProviderServiceCategoryId == service.InternalId && value.RequirementAssignmentId == item.Assignment.Id, cancellationToken);
                var linkedDocument = verification?.ProviderDocumentId is long documentId ? documents.SingleOrDefault(document => document.InternalId == documentId)?.DocumentTypeName : null;
                comparisons.Add(new(item.Assignment.PublicId, item.Definition.Name, item.Assignment.IsRequired, item.Assignment.VerificationRequired, item.Assignment.ExpiryCheckRequired,
                    evidenceNames, verification?.VerificationStatusCode ?? "검증결과 없음", linkedDocument, verification?.VerifiedAt, verification?.ExpiresAt, verification?.RejectionReason));
            }
            var approval = await dbContext.ProviderServiceApprovals.AsNoTracking().SingleOrDefaultAsync(value => value.ProviderServiceCategoryId == service.InternalId, cancellationToken);
            var approvalEvents = await dbContext.ProviderServiceApprovalEvents.AsNoTracking().Where(value => value.ProviderServiceCategoryId == service.InternalId)
                .OrderByDescending(value => value.DecidedAt).Select(value => new AdminProviderServiceApprovalEventResponse(value.FromStatusCode, value.ToStatusCode, value.ActionCode, value.DecisionReason, value.DecidedAt)).ToListAsync(cancellationToken);
            var serviceReviewStage = approval?.ApprovalStatusCode == "PENDING" && approvalEvents.FirstOrDefault()?.ActionCode == "RESUBMIT" ? "SUPPLEMENTED" : approval?.ApprovalStatusCode ?? "PENDING";
            reviews.Add(new(service.ServiceId, service.ServiceName, service.RegistrationStatusCode,
                serviceReviewStage, approval?.ApprovalRequestedAt ?? service.ActivatedAt, approval?.ApprovalDecidedAt,
                approval?.DecisionReason, approval is null ? string.Empty : Convert.ToBase64String(approval.RowVersion), approvalEvents,
                assignments.Count > 0, policy?.RequiredQualificationSummaryText ?? "기준 없음", policy?.InsuranceRequirementText ?? "기준 없음", policy?.SafetyGradeCode ?? "기준 없음", comparisons));
        }
        var quoteRows = await (from quote in dbContext.Quotes.AsNoTracking() join request in dbContext.ServiceRequests.AsNoTracking() on quote.ServiceRequestId equals request.Id join category in dbContext.ServiceCategories.AsNoTracking() on request.CategoryId equals category.Id where quote.ProviderProfileId == identity.Provider.Id orderby quote.CreatedAt descending select new { quote, request, category.Name }).ToListAsync(cancellationToken);
        var quoteIds = quoteRows.Select(row => row.quote.Id).ToArray();
        var revisions = await dbContext.QuoteRevisions.AsNoTracking().Where(value => quoteIds.Contains(value.QuoteId)).GroupBy(value => value.QuoteId).Select(group => group.OrderByDescending(value => value.RevisionNo).First()).ToListAsync(cancellationToken);
        var quotes = quoteRows.Select(row => { var revision = revisions.SingleOrDefault(value => value.QuoteId == row.quote.Id); return new AdminProviderQuoteResponse(row.quote.PublicId, row.request.PublicId, row.request.Title, row.Name, row.quote.SubmittedAt, revision?.TotalAmount, revision?.CurrencyCode ?? "KRW", row.quote.StatusCode, row.quote.StatusCode == "ACCEPTED"); }).ToArray();
        var transactions = await (from transaction in dbContext.Transactions.AsNoTracking() join category in dbContext.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id where transaction.ProviderProfileId == identity.Provider.Id orderby transaction.CreatedAt descending select new AdminProviderTransactionResponse(transaction.PublicId, category.Name, transaction.StatusCode, transaction.AgreedAmount, transaction.CurrencyCode, transaction.StartedAt, transaction.CompletedAt)).ToListAsync(cancellationToken);
        var approvalHistory = await dbContext.ProviderApprovalEvents.AsNoTracking().Where(value => value.ProviderProfileId == identity.Provider.Id).OrderByDescending(value => value.DecidedAt).Select(value => new AdminProviderApprovalEventResponse(value.FromStatusCode, value.ToStatusCode, value.ActionCode, value.Reason, value.DecidedAt)).ToListAsync(cancellationToken);
        var audit = await dbContext.AuditLogs.AsNoTracking().Where(value => value.EntityPublicId == identity.Provider.PublicId || value.EntityPublicId == identity.User.PublicId).OrderByDescending(value => value.OccurredAt).Take(100).Select(value => new AdminCustomerAuditResponse(value.OccurredAt, value.ActionCode, value.EntityType, value.ResultCode, value.Reason, value.ActorRoleCode)).ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow); var soon = today.AddDays(30);
        var reviewStage = identity.Provider.ApprovalStatusCode == "PENDING" && approvalHistory.FirstOrDefault()?.ActionCode == "RESUBMIT" ? "SUPPLEMENTED" : identity.Provider.ApprovalStatusCode;
        return new(new(identity.Provider.PublicId, identity.User.PublicId, identity.Provider.BusinessName, identity.User.Phone, identity.User.Email ?? identity.Provider.PublicEmail, identity.User.StatusCode, reviewStage, reviewStage, identity.Provider.ActivityStatusCode, identity.User.CreatedAt, identity.User.LastLoginAt, roles, Convert.ToBase64String(identity.Provider.RowVersion)),
            new(services.Length, null, null, documents.Count, null, documents.Count(value => value.ExpiresAt >= today && value.ExpiresAt <= soon), documents.Count(value => value.ExpiresAt < today), quotes.Length, quotes.Count(value => value.IsAccepted), transactions.Count(value => value.StatusCode == "COMPLETED"), Max(quotes.Select(value => value.SubmittedAt).Max(), transactions.Select(value => value.CompletedAt ?? value.StartedAt).Max())),
            new(identity.Provider.ProviderTypeCode, identity.Provider.BusinessName, identity.Provider.BusinessRegistrationNo,
                identity.Provider.RepresentativeName, identity.Provider.ContactName, identity.Provider.BusinessAddress,
                identity.Provider.BusinessTypeText, identity.Provider.BusinessItemText, identity.Provider.Introduction,
                identity.Provider.PublicIntroductionHtml, identity.Provider.PublicPhone, identity.Provider.PublicEmail,
                identity.Provider.PublicAddress, identity.Provider.PublicBlogUrl, identity.Provider.PublicWebsiteUrl,
                identity.Provider.PublicLogoUrl, ParseUrls(identity.Provider.PublicPhotoUrlsJson), true),
            services, areas, documents, reviews, quotes, transactions, approvalHistory, audit);
    }

    public async Task<AdminProviderApprovalDecisionResponse> DecideApprovalAsync(Guid providerPublicId, AdminProviderApprovalDecisionRequest request, Guid actorPublicId, CancellationToken token)
    {
        var action=request.ActionCode.Trim().ToUpperInvariant();
        if(action is not ("APPROVE" or "REJECT"))throw new AdminServiceCategoryException("ADMIN_PROVIDER_APPROVAL_ACTION_INVALID","전체 승인 또는 반려를 선택해 주세요.");
        var reason=string.IsNullOrWhiteSpace(request.Reason)?null:request.Reason.Trim();
        if(action=="REJECT"&&reason is null)throw new AdminServiceCategoryException("ADMIN_PROVIDER_REJECTION_REASON_REQUIRED","전체 승인 반려 사유를 입력해 주세요.");
        if(reason?.Length>1000)throw new AdminServiceCategoryException("ADMIN_PROVIDER_DECISION_REASON_TOO_LONG","심사 사유는 1,000자 이하여야 합니다.");
        byte[] version;try{version=Convert.FromBase64String(request.RowVersion);}catch(FormatException){throw ApprovalConflict();}
        var row=await(from provider in dbContext.ProviderProfiles join user in dbContext.Users on provider.UserId equals user.Id where provider.PublicId==providerPublicId select new{Provider=provider,User=user}).SingleOrDefaultAsync(token)
            ??throw new AdminServiceCategoryException("ADMIN_PROVIDER_NOT_FOUND","전문가를 찾을 수 없습니다.",StatusCodes.Status404NotFound);
        if(!row.Provider.RowVersion.SequenceEqual(version))throw ApprovalConflict();
        var actor=await dbContext.Users.Where(x=>x.PublicId==actorPublicId&&x.StatusCode=="ACTIVE").Select(x=>(long?)x.Id).SingleOrDefaultAsync(token)
            ??throw new AdminServiceCategoryException("ADMIN_USER_NOT_FOUND","현재 관리자 계정을 확인할 수 없습니다.",StatusCodes.Status401Unauthorized);
        if(action=="APPROVE")
        {
            if(row.User.StatusCode!="ACTIVE")throw new AdminServiceCategoryException("ADMIN_PROVIDER_ACCOUNT_INACTIVE","정상 계정인 전문가만 승인할 수 있습니다.",StatusCodes.Status409Conflict);
            var duplicateCheckOnly=environment.IsDevelopment()||configuration.GetValue<bool>("Prelaunch:AllowPhoneDuplicateCheckOnly");
            var phoneAccepted=row.User.PhoneVerificationStatusCode=="VERIFIED"||duplicateCheckOnly&&row.User.PhoneVerificationStatusCode=="NOT_INTEGRATED";
            if(!phoneAccepted||string.IsNullOrWhiteSpace(row.User.Phone))throw new AdminServiceCategoryException("ADMIN_PROVIDER_VERIFIED_PHONE_REQUIRED",duplicateCheckOnly?"휴대폰 번호 중복확인을 완료한 전문가만 승인할 수 있습니다.":"휴대폰 본인인증이 완료된 전문가만 승인할 수 있습니다.",StatusCodes.Status409Conflict);
            if(string.IsNullOrWhiteSpace(row.Provider.Introduction))throw new AdminServiceCategoryException("ADMIN_PROVIDER_INTRODUCTION_REQUIRED","심사용 소개를 작성한 전문가만 승인할 수 있습니다.",StatusCodes.Status409Conflict);
            var services=await dbContext.ProviderServiceCategories.Where(x=>x.ProviderProfileId==row.Provider.Id&&x.StatusCode=="ACTIVE").Select(x=>x.Id).ToArrayAsync(token);
            if(services.Length==0)throw new AdminServiceCategoryException("ADMIN_PROVIDER_ACTIVE_SERVICE_REQUIRED","승인할 활성 서비스가 한 개 이상 필요합니다.",StatusCodes.Status409Conflict);
        }
        var before=row.Provider.ApprovalStatusCode;var after=action=="APPROVE"?"APPROVED":"REJECTED";var now=DateTime.UtcNow;
        row.Provider.ApprovalStatusCode=after;row.Provider.ActivityStatusCode=action=="APPROVE"?"ACTIVE":"INACTIVE";row.Provider.ApprovalDecidedAt=now;row.Provider.ApprovalDecidedByUserId=actor;row.Provider.UpdatedAt=now;row.Provider.UpdatedByUserId=actor;
        dbContext.Entry(row.Provider).Property(x=>x.RowVersion).OriginalValue=version;
        dbContext.ProviderApprovalEvents.Add(new ProviderApprovalEvent{ProviderProfileId=row.Provider.Id,FromStatusCode=before,ToStatusCode=after,ActionCode=action,Reason=reason,DecidedAt=now,DecidedByUserId=actor});
        dbContext.AuditLogs.Add(new AuditLog{OccurredAt=now,ActorUserId=actor,ActorRoleCode=RoleCodes.Admin,ActionCode=action=="APPROVE"?"PROVIDER_APPROVED":"PROVIDER_REJECTED",EntityType="PROVIDER_PROFILE",EntityPublicId=row.Provider.PublicId,ResultCode="SUCCESS",Reason=reason,BeforeJson=JsonSerializer.Serialize(new{ApprovalStatusCode=before}),AfterJson=JsonSerializer.Serialize(new{ApprovalStatusCode=after,row.Provider.ActivityStatusCode})});
        try{await dbContext.SaveChangesAsync(token);}catch(DbUpdateConcurrencyException){throw ApprovalConflict();}
        await matchingService.RefreshProviderMatchesAsync(row.Provider.Id, token);
        return new(after,row.Provider.ActivityStatusCode,now,reason,Convert.ToBase64String(row.Provider.RowVersion));
    }

    public async Task<AdminProviderDetailResponse> UpdateAsync(Guid providerPublicId, AdminUpdateProviderRequest request, Guid actorPublicId, CancellationToken token)
    {
        var row = await dbContext.ProviderProfiles.SingleOrDefaultAsync(x => x.PublicId == providerPublicId, token)
            ?? throw new AdminServiceCategoryException("ADMIN_PROVIDER_NOT_FOUND", "전문가를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var type = request.ProviderTypeCode.Trim().ToUpperInvariant();
        if (type is not ("BUSINESS" or "INDIVIDUAL")) throw new AdminServiceCategoryException("ADMIN_PROVIDER_TYPE_INVALID", "사업자 또는 개인 전문가를 선택해 주세요.");
        if (string.IsNullOrWhiteSpace(request.BusinessName) || string.IsNullOrWhiteSpace(request.RepresentativeName) || string.IsNullOrWhiteSpace(request.ContactName))
            throw new AdminServiceCategoryException("ADMIN_PROVIDER_REQUIRED_FIELDS", "전문가명, 대표자·본인 성명, 담당자명을 입력해 주세요.");
        byte[] version; try { version = Convert.FromBase64String(request.RowVersion); } catch (FormatException) { throw ApprovalConflict(); }
        if (!row.RowVersion.SequenceEqual(version)) throw ApprovalConflict();
        var actor = await dbContext.Users.Where(x => x.PublicId == actorPublicId && x.StatusCode == "ACTIVE").Select(x => (long?)x.Id).SingleOrDefaultAsync(token)
            ?? throw new AdminServiceCategoryException("ADMIN_USER_NOT_FOUND", "현재 관리자 계정을 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);
        var number = Digits(request.BusinessRegistrationNo);
        if (type == "BUSINESS" && number?.Length != 10) throw new AdminServiceCategoryException("ADMIN_BUSINESS_NUMBER_REQUIRED", "사업자 전문가는 사업자등록번호 10자리가 필요합니다.");
        if (type == "INDIVIDUAL") number = null;
        if (number is not null && await dbContext.ProviderProfiles.AnyAsync(x => x.Id != row.Id && x.BusinessRegistrationNo == number, token))
            throw new AdminServiceCategoryException("ADMIN_BUSINESS_NUMBER_DUPLICATE", "이미 등록된 사업자등록번호입니다.", StatusCodes.Status409Conflict);
        var before = JsonSerializer.Serialize(new { row.ProviderTypeCode, row.BusinessName, row.BusinessRegistrationNo });
        row.ProviderTypeCode = type; row.BusinessName = request.BusinessName.Trim(); row.RepresentativeName = request.RepresentativeName.Trim(); row.ContactName = request.ContactName.Trim();
        row.BusinessRegistrationNo = number; row.BusinessAddress = type == "BUSINESS" ? Clean(request.BusinessAddress, 500) : null;
        row.BusinessTypeText = type == "BUSINESS" ? Clean(request.BusinessTypeText, 100) : null; row.BusinessItemText = type == "BUSINESS" ? Clean(request.BusinessItemText, 100) : null;
        row.Introduction = Clean(request.Introduction, 1000); row.PublicPhone = Clean(request.PublicPhone, 30);
        row.PublicEmail = Clean(request.PublicEmail, 320); row.PublicAddress = Clean(request.PublicAddress, 500); row.PublicBlogUrl = Clean(request.PublicBlogUrl, 1000); row.PublicWebsiteUrl = Clean(request.PublicWebsiteUrl, 1000);
        row.UpdatedAt = DateTime.UtcNow; row.UpdatedByUserId = actor; dbContext.Entry(row).Property(x => x.RowVersion).OriginalValue = version;
        dbContext.AuditLogs.Add(new AuditLog { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = "PROVIDER_PROFILE_UPDATED", EntityType = "PROVIDER_PROFILE", EntityPublicId = row.PublicId, ResultCode = "SUCCESS", BeforeJson = before, AfterJson = JsonSerializer.Serialize(new { row.ProviderTypeCode, row.BusinessName, row.BusinessRegistrationNo }) });
        try { await dbContext.SaveChangesAsync(token); } catch (DbUpdateConcurrencyException) { throw ApprovalConflict(); }
        return await GetAsync(providerPublicId, token) ?? throw ApprovalConflict();
    }

    public async Task<AdminProviderDetailResponse> ReplaceServicesAsync(Guid providerPublicId, AdminReplaceProviderServicesRequest request, Guid actorPublicId, CancellationToken token)
    {
        var ids = request.CategoryIds ?? [];
        if (ids.Count != ids.Distinct().Count()) throw new AdminServiceCategoryException("ADMIN_PROVIDER_SERVICE_DUPLICATE", "같은 서비스를 중복 선택할 수 없습니다.");
        var actor = await ResolveActorAsync(actorPublicId, token);
        var provider = await dbContext.ProviderProfiles.SingleOrDefaultAsync(x => x.PublicId == providerPublicId, token)
            ?? throw new AdminServiceCategoryException("ADMIN_PROVIDER_NOT_FOUND", "전문가를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var categories = await dbContext.ServiceCategories.Where(x => ids.Contains(x.PublicId) && x.LevelCode == "SERVICE" && x.StatusCode == "ACTIVE").ToListAsync(token);
        if (categories.Count != ids.Count) throw new AdminServiceCategoryException("ADMIN_PROVIDER_SERVICE_INVALID", "현재 사용 가능한 하위 서비스만 선택할 수 있습니다.");
        var now = DateTime.UtcNow;
        var existing = await dbContext.ProviderServiceCategories.Where(x => x.ProviderProfileId == provider.Id).ToListAsync(token);
        var desired = categories.Select(x => x.Id).ToHashSet();
        foreach (var item in existing)
        {
            var active = desired.Contains(item.CategoryId); var wasActive = item.StatusCode == "ACTIVE";
            item.StatusCode = active ? "ACTIVE" : "INACTIVE"; item.IsNationwide = active && item.IsNationwide;
            item.ActivatedAt = active && !wasActive ? now : item.ActivatedAt; item.DeactivatedAt = active ? null : item.DeactivatedAt ?? now;
            item.UpdatedAt = now; item.UpdatedByUserId = actor;
        }
        foreach (var category in categories.Where(x => existing.All(y => y.CategoryId != x.Id)))
            dbContext.ProviderServiceCategories.Add(new ProviderServiceCategory { ProviderProfileId = provider.Id, CategoryId = category.Id, StatusCode = "ACTIVE", ActivatedAt = now, CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor });
        await dbContext.SaveChangesAsync(token);
        var activeServices = await dbContext.ProviderServiceCategories.Where(x => x.ProviderProfileId == provider.Id && x.StatusCode == "ACTIVE").ToListAsync(token);
        var activeIds = activeServices.Select(x => x.Id).ToArray();
        var approvals = await dbContext.ProviderServiceApprovals.Where(x => activeIds.Contains(x.ProviderServiceCategoryId)).ToListAsync(token);
        foreach (var service in activeServices)
        {
            var approval = approvals.SingleOrDefault(x => x.ProviderServiceCategoryId == service.Id);
            if (approval is null) dbContext.ProviderServiceApprovals.Add(new ProviderServiceApproval { ProviderServiceCategoryId = service.Id, ApprovalStatusCode = "APPROVED", ApprovalRequestedAt = now, ApprovalDecidedAt = now, DecisionReason = "관리자 서비스 등록 자동 승인", CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor });
            else { approval.ApprovalStatusCode = "APPROVED"; approval.ApprovalDecidedAt = now; approval.ApprovalDecidedByUserId = actor; approval.DecisionReason = "관리자 서비스 등록 자동 승인"; approval.UpdatedAt = now; approval.UpdatedByUserId = actor; }
        }
        var inactiveIds = existing.Where(x => x.StatusCode == "INACTIVE").Select(x => x.Id).ToArray();
        var inactiveAreas = await dbContext.ProviderServiceAreas.Where(x => inactiveIds.Contains(x.ProviderServiceCategoryId) && x.StatusCode == "ACTIVE").ToListAsync(token);
        foreach (var area in inactiveAreas) { area.StatusCode = "INACTIVE"; area.DeactivatedAt = now; area.UpdatedAt = now; area.UpdatedByUserId = actor; }
        dbContext.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = "PROVIDER_SERVICES_UPDATED", EntityType = "PROVIDER_PROFILE", EntityPublicId = provider.PublicId, ResultCode = "SUCCESS", AfterJson = JsonSerializer.Serialize(new { CategoryIds = ids }) });
        await dbContext.SaveChangesAsync(token); await matchingService.RefreshProviderMatchesAsync(provider.Id, token);
        return await GetAsync(providerPublicId, token) ?? throw ApprovalConflict();
    }

    public async Task<AdminProviderDetailResponse> ReplaceAreasAsync(Guid providerPublicId, AdminReplaceProviderAreasRequest request, Guid actorPublicId, CancellationToken token)
    {
        var selections = request.Services ?? [];
        if (selections.Count != selections.Select(x => x.ServiceCategoryId).Distinct().Count() || selections.Any(x => (x.AdministrativeAreaIds ?? []).Count != (x.AdministrativeAreaIds ?? []).Distinct().Count()))
            throw new AdminServiceCategoryException("ADMIN_PROVIDER_AREA_DUPLICATE", "같은 서비스 또는 활동지역을 중복 선택할 수 없습니다.");
        var actor = await ResolveActorAsync(actorPublicId, token);
        var provider = await dbContext.ProviderProfiles.SingleOrDefaultAsync(x => x.PublicId == providerPublicId, token)
            ?? throw new AdminServiceCategoryException("ADMIN_PROVIDER_NOT_FOUND", "전문가를 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        var services = await (from link in dbContext.ProviderServiceCategories join category in dbContext.ServiceCategories on link.CategoryId equals category.Id where link.ProviderProfileId == provider.Id && link.StatusCode == "ACTIVE" select new { Link = link, category.PublicId }).ToListAsync(token);
        if (selections.Any(x => services.All(y => y.PublicId != x.ServiceCategoryId))) throw new AdminServiceCategoryException("ADMIN_PROVIDER_AREA_SERVICE_INVALID", "활성 서비스에 대해서만 활동지역을 수정할 수 있습니다.");
        var areaIds = selections.SelectMany(x => x.AdministrativeAreaIds ?? []).Distinct().ToArray();
        var areas = await dbContext.AdministrativeAreas.Where(x => areaIds.Contains(x.PublicId) && x.AreaLevelCode == "SIGUNGU" && x.IsActive).ToListAsync(token);
        if (areas.Count != areaIds.Length) throw new AdminServiceCategoryException("ADMIN_PROVIDER_AREA_INVALID", "현재 사용 가능한 시·군·구만 선택할 수 있습니다.");
        var links = await dbContext.ProviderServiceAreas.Where(x => services.Select(y => y.Link.Id).Contains(x.ProviderServiceCategoryId)).ToListAsync(token);
        var now = DateTime.UtcNow;
        foreach (var service in services)
        {
            var wanted = selections.SingleOrDefault(x => x.ServiceCategoryId == service.PublicId)?.AdministrativeAreaIds ?? [];
            var wantedInternal = areas.Where(x => wanted.Contains(x.PublicId)).Select(x => x.Id).ToHashSet();
            foreach (var link in links.Where(x => x.ProviderServiceCategoryId == service.Link.Id)) { var active = wantedInternal.Contains(link.AdministrativeAreaId); var wasActive = link.StatusCode == "ACTIVE"; link.StatusCode = active ? "ACTIVE" : "INACTIVE"; link.ActivatedAt = active && !wasActive ? now : link.ActivatedAt; link.DeactivatedAt = active ? null : link.DeactivatedAt ?? now; link.UpdatedAt = now; link.UpdatedByUserId = actor; }
            foreach (var areaId in wantedInternal.Where(x => links.All(y => y.ProviderServiceCategoryId != service.Link.Id || y.AdministrativeAreaId != x))) dbContext.ProviderServiceAreas.Add(new ProviderServiceArea { ProviderServiceCategoryId = service.Link.Id, AdministrativeAreaId = areaId, StatusCode = "ACTIVE", ActivatedAt = now, CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor });
        }
        dbContext.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = "PROVIDER_AREAS_UPDATED", EntityType = "PROVIDER_PROFILE", EntityPublicId = provider.PublicId, ResultCode = "SUCCESS", AfterJson = JsonSerializer.Serialize(selections) });
        await dbContext.SaveChangesAsync(token); await matchingService.RefreshProviderMatchesAsync(provider.Id, token);
        return await GetAsync(providerPublicId, token) ?? throw ApprovalConflict();
    }

    public async Task<(Stream Content, string ContentType, string FileName)> OpenDocumentAsync(Guid providerPublicId, Guid filePublicId, Guid actorPublicId, CancellationToken token)
    {
        var actor = await dbContext.Users.Where(x => x.PublicId == actorPublicId && x.StatusCode == "ACTIVE").Select(x => (long?)x.Id).SingleOrDefaultAsync(token)
            ?? throw new AdminServiceCategoryException("ADMIN_USER_NOT_FOUND", "현재 관리자 계정을 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);
        var file = await (from provider in dbContext.ProviderProfiles.AsNoTracking() join document in dbContext.ProviderDocuments.AsNoTracking() on provider.Id equals document.ProviderProfileId join stored in dbContext.Files.AsNoTracking() on document.FileId equals stored.Id where provider.PublicId == providerPublicId && stored.PublicId == filePublicId && stored.StatusCode == "ACTIVE" select stored).SingleOrDefaultAsync(token)
            ?? throw new AdminServiceCategoryException("ADMIN_PROVIDER_DOCUMENT_NOT_FOUND", "전문가 증빙 파일을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        dbContext.AuditLogs.Add(new AuditLog { OccurredAt = DateTime.UtcNow, ActorUserId = actor, ActorRoleCode = RoleCodes.Admin, ActionCode = "PROVIDER_DOCUMENT_VIEWED", EntityType = "STORED_FILE", EntityPublicId = file.PublicId, ResultCode = "SUCCESS", Reason = "전문가 승인 증빙 열람" });
        await dbContext.SaveChangesAsync(token);
        return (await fileStorage.OpenReadAsync(file.StorageKey, token), file.ContentType, file.OriginalFileName);
    }

    private async Task<long> ResolveActorAsync(Guid actorPublicId, CancellationToken token) => await dbContext.Users.Where(x => x.PublicId == actorPublicId && x.StatusCode == "ACTIVE").Select(x => (long?)x.Id).SingleOrDefaultAsync(token)
        ?? throw new AdminServiceCategoryException("ADMIN_USER_NOT_FOUND", "현재 관리자 계정을 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);

    private static string? MaskPhone(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var digits = new string(value.Where(char.IsDigit).ToArray()); return digits.Length < 7 ? "연락처 등록됨" : $"{digits[..3]}-****-{digits[^4..]}"; }
    private static string? MaskEmail(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var at = value.IndexOf('@'); return at <= 0 ? "이메일 등록됨" : $"{value[0]}***{value[at..]}"; }
    private static string? MaskBusinessNo(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var digits = new string(value.Where(char.IsDigit).ToArray()); return digits.Length == 10 ? $"{digits[..3]}-**-{digits[^5..]}" : "사업자등록번호 등록됨"; }
    private static DateTime? Max(DateTime? left, DateTime? right) => left.HasValue && right.HasValue ? (left > right ? left : right) : left ?? right;
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : throw new AdminServiceCategoryException("ADMIN_PROVIDER_VALUE_TOO_LONG", $"{max}자 이하로 입력해 주세요.");
    private static string? Digits(string? value) => string.IsNullOrWhiteSpace(value) ? null : new string(value.Where(char.IsDigit).ToArray());
    private static IReadOnlyList<string> ParseUrls(string? json) { if (string.IsNullOrWhiteSpace(json)) return []; try { return JsonSerializer.Deserialize<string[]>(json) ?? []; } catch (JsonException) { return []; } }
    private static int RoleOrder(string role) => role switch { "CUSTOMER" => 0, "PROVIDER" => 1, "ADMIN" => 2, _ => 9 };
    private static AdminServiceCategoryException ApprovalConflict()=>new("ADMIN_PROVIDER_APPROVAL_CONFLICT","다른 관리자가 먼저 승인 상태를 변경했습니다. 최신 상태를 다시 확인해 주세요.",StatusCodes.Status409Conflict);
}
