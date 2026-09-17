using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.FilePrivacy;
using SoodalLife.Api.Features.Work;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Subscriptions;

public sealed class CustomerCareSubscriptionService(SoodalLifeDbContext db, IPrivateFileStorage storage, ICrossDomainFilePublicationResolver filePublication, SubscriptionTerminationService terminationService,ISubscriptionPaymentGateway paymentGateway,IOptions<SubscriptionPaymentGatewayOptions> paymentOptions,ISubscriptionPaymentTokenProtector tokenProtector,SubscriptionPaymentProcessor paymentProcessor)
{
    public async Task<IReadOnlyList<SubscriptionServiceItem>> EligibleServices(CancellationToken token)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await (from service in db.ServiceCategories.AsNoTracking()
                      join middle in db.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                      join major in db.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                      where service.LevelCode == "SERVICE" && service.StatusCode == "ACTIVE" &&
                            db.CategoryOperationPolicies.Any(policy => policy.CategoryId == service.Id && policy.IsActive &&
                                policy.EffectiveFrom <= today && (policy.EffectiveTo == null || policy.EffectiveTo > today) &&
                                policy.SubscriptionOptionText == "허용")
                      orderby major.SortOrder, middle.SortOrder, service.SortOrder
                      select new SubscriptionServiceItem(service.PublicId, major.Name, middle.Name, service.Name,
                          $"{major.Name} > {middle.Name} > {service.Name}")).ToListAsync(token);
    }

    public async Task<IReadOnlyList<CustomerCareProductResponse>> Products(Guid? serviceId, CancellationToken token)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = from product in db.CareProducts.AsNoTracking()
                    join service in db.ServiceCategories.AsNoTracking() on product.ServiceCategoryId equals service.Id
                    where product.IsActive && product.EffectiveFrom <= today && (product.EffectiveTo == null || product.EffectiveTo >= today) &&
                          service.StatusCode == "ACTIVE" &&
                          db.CategoryOperationPolicies.Any(policy => policy.CategoryId == service.Id && policy.IsActive &&
                              policy.EffectiveFrom <= today && (policy.EffectiveTo == null || policy.EffectiveTo > today) &&
                              policy.SubscriptionOptionText == "허용")
                    select new { product, service };
        if (serviceId.HasValue) query = query.Where(row => row.service.PublicId == serviceId.Value);
        return await query.OrderBy(row => row.service.SortOrder).ThenBy(row => row.product.ProductName)
            .Select(row => new CustomerCareProductResponse(row.product.PublicId, row.service.PublicId, row.service.Name,
                row.product.ProductName, row.product.Description, row.product.ServiceScopeText, row.product.VisitsPerPeriod,
                row.product.ExpectedDurationMinutes, row.product.BillingPeriodCode, row.product.StandardMonthlyAmount,
                row.product.StandardVisitAmount, row.product.EffectiveFrom, row.product.EffectiveTo)).ToListAsync(token);
    }

    public async Task<CustomerCareHomeResponse> Home(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var eligible = await EligibleServices(token);
        var products = await Products(null, token);
        var upcoming = await VisitQuery(identity.ProfileId).Where(row => row.visit.ScheduledStartAt >= DateTime.UtcNow &&
            row.visit.StatusCode != "CANCELLED" && row.visit.StatusCode != "SKIPPED").OrderBy(row => row.visit.ScheduledStartAt)
            .Take(3).ToListAsync(token);
        var completed = await VisitQuery(identity.ProfileId).Where(row => row.visit.CustomerConfirmedAt != null)
            .OrderByDescending(row => row.visit.CustomerConfirmedAt).Take(3).ToListAsync(token);
        return new CustomerCareHomeResponse(eligible.Count, products.Count,
            await db.SubscriptionRequests.CountAsync(item => item.CustomerProfileId == identity.ProfileId &&
                db.SubscriptionRecurrenceRules.Any(rule => rule.SubscriptionRequestId == item.Id) &&
                db.ServiceCategories.Any(category => category.Id == item.ServiceCategoryId) &&
                db.AdministrativeAreas.Any(area => area.Id == item.AdministrativeAreaId) &&
                (item.StatusCode == "OPEN" || (item.StatusCode == "CONTRACTED" && db.SubscriptionContracts.Any(contract =>
                    contract.SubscriptionRequestId == item.Id && (contract.StatusCode == "PAYMENT_PENDING" || contract.StatusCode == "ACTIVE" ||
                    contract.StatusCode == "PAUSED" || contract.StatusCode == "TERMINATION_REQUESTED")))), token),
            await db.SubscriptionContracts.CountAsync(item => item.CustomerProfileId == identity.ProfileId &&
                (item.StatusCode == "PAYMENT_PENDING" || item.StatusCode == "ACTIVE" || item.StatusCode == "PAUSED" || item.StatusCode == "TERMINATION_REQUESTED"), token),
            await db.SubscriptionVisitSchedules.CountAsync(visit => visit.ScheduledStartAt >= DateTime.UtcNow && visit.StatusCode != "CANCELLED" && visit.StatusCode != "SKIPPED" &&
                db.SubscriptionContracts.Any(contract => contract.Id == visit.SubscriptionContractId && contract.CustomerProfileId == identity.ProfileId), token),
            await db.SubscriptionVisitSchedules.CountAsync(visit => visit.CustomerConfirmedAt != null &&
                db.SubscriptionContracts.Any(contract => contract.Id == visit.SubscriptionContractId && contract.CustomerProfileId == identity.ProfileId), token),
            await (from recipient in db.NotificationRecipients
                   join notification in db.Notifications on recipient.NotificationId equals notification.Id
                   where recipient.UserId == identity.UserId && recipient.ReadAt == null && recipient.ArchivedAt == null &&
                         (notification.SubscriptionContractId != null || notification.SubscriptionVisitScheduleId != null ||
                          notification.TargetTypeCode == "SubscriptionRequest" || notification.TargetTypeCode == "SubscriptionContract" ||
                          notification.TargetTypeCode == "SubscriptionVisitSchedule")
                   select recipient).CountAsync(token),
            upcoming.Select(MapVisit).ToArray(), completed.Select(MapVisit).ToArray());
    }

    public async Task<IReadOnlyList<CustomerSubscriptionRequestResponse>> Requests(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var ids = await db.SubscriptionRequests.AsNoTracking().Where(item => item.CustomerProfileId == identity.ProfileId &&
                db.SubscriptionRecurrenceRules.Any(rule => rule.SubscriptionRequestId == item.Id) &&
                db.ServiceCategories.Any(category => category.Id == item.ServiceCategoryId) &&
                db.AdministrativeAreas.Any(area => area.Id == item.AdministrativeAreaId))
            .OrderByDescending(item => item.CreatedAt).Select(item => item.Id).ToListAsync(token);
        var result = new List<CustomerSubscriptionRequestResponse>();
        foreach (var id in ids) result.Add(await Request(id, identity.ProfileId, token));
        return result;
    }

    public async Task<CustomerSubscriptionRequestResponse> Request(Guid id, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var internalId = await db.SubscriptionRequests.Where(item => item.PublicId == id && item.CustomerProfileId == identity.ProfileId)
            .Select(item => (long?)item.Id).SingleOrDefaultAsync(token) ?? throw NotFound("SUBSCRIPTION_REQUEST_NOT_FOUND", "구독 요청을 찾을 수 없습니다.");
        return await Request(internalId, identity.ProfileId, token);
    }

    public async Task<CustomerSubscriptionRequestResponse> CancelRequest(Guid id, CancelSubscriptionRequestRequest input, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var request = await db.SubscriptionRequests.SingleOrDefaultAsync(item => item.PublicId == id && item.CustomerProfileId == identity.ProfileId, token)
                      ?? throw NotFound("SUBSCRIPTION_REQUEST_NOT_FOUND", "구독 요청을 찾을 수 없습니다.");
        if (request.StatusCode == "CANCELLED") return await Request(request.Id, identity.ProfileId, token);
        if (request.StatusCode != "OPEN" || request.SelectedApplicationId.HasValue)
            throw Conflict("SUBSCRIPTION_REQUEST_CANCEL_INVALID", "전문가를 선택하기 전의 공개 요청만 취소할 수 있습니다.");
        if (await db.SubscriptionEvents.AsNoTracking().AnyAsync(item => item.IdempotencyKey == input.IdempotencyKey, token))
            return await Request(request.Id, identity.ProfileId, token);
        ApplyVersion(request, input.RowVersion);
        var now = DateTime.UtcNow; request.StatusCode = "CANCELLED"; request.UpdatedAt = now; request.UpdatedByUserId = identity.UserId;
        foreach (var application in await db.SubscriptionApplications.Where(item => item.SubscriptionRequestId == request.Id && item.StatusCode == "SUBMITTED").ToListAsync(token))
        { application.StatusCode = "NOT_SELECTED"; application.UpdatedAt = now; application.UpdatedByUserId = identity.UserId; }
        Event(request.Id, null, null, "REQUEST_CANCELLED", identity.UserId, input.IdempotencyKey, now);
        Audit(identity.UserId, "SUBSCRIPTION_REQUEST_CANCELLED", "SUBSCRIPTION_REQUEST", request.PublicId, input.Reason, now);
        await db.SaveChangesAsync(token);
        return await Request(request.Id, identity.ProfileId, token);
    }

    public async Task<IReadOnlyList<CustomerSubscriptionApplicationResponse>> Applications(Guid requestId, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var request = await db.SubscriptionRequests.AsNoTracking().SingleOrDefaultAsync(item => item.PublicId == requestId && item.CustomerProfileId == identity.ProfileId, token)
            ?? throw NotFound("SUBSCRIPTION_REQUEST_NOT_FOUND", "구독 요청을 찾을 수 없습니다.");
        var rows = await db.SubscriptionApplications.AsNoTracking().Where(item => item.SubscriptionRequestId == request.Id).ToListAsync(token);
        var result = new List<CustomerSubscriptionApplicationResponse>();
        foreach (var row in rows) result.Add(await Application(row, request, token));
        return result.OrderByDescending(item => item.TrustEvaluationStatus == "CALCULATED")
            .ThenByDescending(item => item.TrustScore).ThenBy(item => item.SubmittedAt).ToArray();
    }

    public async Task<IReadOnlyList<CustomerSubscriptionContractResponse>> Contracts(ClaimsPrincipal principal, string? status, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var query = db.SubscriptionContracts.AsNoTracking().Where(item => item.CustomerProfileId == identity.ProfileId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.StatusCode == status.Trim().ToUpperInvariant());
        var ids = await query.OrderByDescending(item => item.UpdatedAt).Select(item => item.Id).ToListAsync(token);
        var result = new List<CustomerSubscriptionContractResponse>();
        foreach (var id in ids) result.Add(await Contract(id, identity.ProfileId, token));
        return result;
    }

    public async Task<CustomerSubscriptionContractResponse> Contract(Guid id, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var internalId = await db.SubscriptionContracts.Where(item => item.PublicId == id && item.CustomerProfileId == identity.ProfileId)
            .Select(item => (long?)item.Id).SingleOrDefaultAsync(token) ?? throw NotFound("SUBSCRIPTION_CONTRACT_NOT_FOUND", "구독 계약을 찾을 수 없습니다.");
        return await Contract(internalId, identity.ProfileId, token);
    }

    public async Task<IReadOnlyList<CustomerSubscriptionVisitListItem>> Visits(ClaimsPrincipal principal, Guid? contractId, string? status, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var query = VisitQuery(identity.ProfileId);
        if (contractId.HasValue) query = query.Where(row => row.contract.PublicId == contractId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(row => row.visit.StatusCode == status.Trim().ToUpperInvariant());
        return (await query.OrderByDescending(row => row.visit.ScheduledStartAt).ToListAsync(token)).Select(MapVisit).ToArray();
    }

    public async Task<CustomerSubscriptionVisitDetailResponse> Visit(Guid id, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var row = await VisitQuery(identity.ProfileId).SingleOrDefaultAsync(value => value.visit.PublicId == id, token)
            ?? throw NotFound("SUBSCRIPTION_VISIT_NOT_FOUND", "구독 회차를 찾을 수 없습니다.");
        var fileRows = await (from link in db.SubscriptionVisitFiles.AsNoTracking()
                              join file in db.Files.AsNoTracking() on link.FileId equals file.Id
                              where link.SubscriptionVisitScheduleId == row.visit.Id && file.StatusCode == "ACTIVE"
                              orderby link.DisplayOrder
                              select new { Link = link, File = file }).ToListAsync(token);
        var files = new List<CustomerSubscriptionVisitFileResponse>(fileRows.Count);
        foreach (var item in fileRows)
        {
            var publication = await filePublication.ResolveAsync(item.File, identity.UserId, true, true, token);
            var published = publication.PublishedFile ?? item.File;
            files.Add(new(item.File.PublicId, publication.Allowed ? published.OriginalFileName : "evidence", published.ContentType, published.SizeBytes,
                item.Link.DisplayOrder,
                publication.Allowed ? $"/api/v1/customers/me/care/visits/{row.visit.PublicId}/files/{item.File.PublicId}" : null,
                publication.StatusCode, publication.Allowed ? null : publication.Message));
        }
        var changes = await db.SubscriptionScheduleChanges.AsNoTracking().Where(item => item.SubscriptionVisitScheduleId == row.visit.Id)
            .OrderByDescending(item => item.RequestedAt).ToListAsync(token);
        return new CustomerSubscriptionVisitDetailResponse(MapVisit(row), row.visit.VisitVerificationResultCode, row.visit.WorkCompletedAt,
            row.visit.CompletionChecklistJson, row.visit.CompletionNote, CustomerProgress(row.visit), files,
            await db.Reviews.Where(item => item.SubscriptionVisitScheduleId == row.visit.Id).Select(item => (Guid?)item.PublicId).SingleOrDefaultAsync(token),
            await db.AfterServiceCases.Where(item => item.SubscriptionVisitScheduleId == row.visit.Id).Select(item => (Guid?)item.PublicId).FirstOrDefaultAsync(token),
            await db.DisputeCases.Where(item => item.SubscriptionVisitScheduleId == row.visit.Id).Select(item => (Guid?)item.PublicId).FirstOrDefaultAsync(token),
            changes.Select(MapChange).ToArray());
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenVisitFile(Guid visitId, Guid fileId, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var file = await (from value in db.Files.AsNoTracking()
                          join link in db.SubscriptionVisitFiles.AsNoTracking() on value.Id equals link.FileId
                          join visit in db.SubscriptionVisitSchedules.AsNoTracking() on link.SubscriptionVisitScheduleId equals visit.Id
                          join contract in db.SubscriptionContracts.AsNoTracking() on visit.SubscriptionContractId equals contract.Id
                          where visit.PublicId == visitId && value.PublicId == fileId && contract.CustomerProfileId == identity.ProfileId && value.StatusCode == "ACTIVE"
                          select value).SingleOrDefaultAsync(token) ?? throw NotFound("SUBSCRIPTION_VISIT_FILE_NOT_FOUND", "회차 증빙파일을 찾을 수 없습니다.");
        var publication = await filePublication.ResolveAsync(file, identity.UserId, true, true, token);
        if (!publication.Allowed) throw new SubscriptionBusinessException(403, "FILE_PRIVACY_BLOCKED", publication.Message);
        var published = publication.PublishedFile!;
        return (await storage.OpenReadAsync(published.StorageKey, token), published.ContentType, published.OriginalFileName);
    }

    public async Task<CustomerSubscriptionContractResponse> ChangeContract(Guid id, string action, CustomerContractActionRequest input, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var contract = await db.SubscriptionContracts.SingleOrDefaultAsync(item => item.PublicId == id && item.CustomerProfileId == identity.ProfileId, token)
            ?? throw NotFound("SUBSCRIPTION_CONTRACT_NOT_FOUND", "구독 계약을 찾을 수 없습니다.");
        var existing = await db.SubscriptionEvents.AsNoTracking().AnyAsync(item => item.IdempotencyKey == input.IdempotencyKey, token);
        if (existing) return await Contract(contract.Id, identity.ProfileId, token);
        ApplyVersion(contract, input.RowVersion);
        var now = DateTime.UtcNow;
        var future = await db.SubscriptionVisitSchedules.Where(item => item.SubscriptionContractId == contract.Id && item.ScheduledStartAt >= now &&
            item.StatusCode != "COMPLETED" && item.StatusCode != "CANCELLED" && item.StatusCode != "SKIPPED").ToListAsync(token);
        var code = action.Trim().ToUpperInvariant();
        if (code is "PAUSE" or "TERMINATE" && string.IsNullOrWhiteSpace(input.Reason))
            throw Bad("CONTRACT_ACTION_REASON_REQUIRED", code == "PAUSE" ? "일시정지 사유를 입력해 주세요." : "해지 사유를 입력해 주세요.");
        if (code == "PAUSE")
        {
            if (contract.StatusCode != "ACTIVE") throw Conflict("CONTRACT_STATE_INVALID", "이용 중인 구독만 일시정지할 수 있습니다.");
            contract.StatusCode = "PAUSED"; contract.PauseStartedAt = now; contract.ResumePlannedAt = input.ResumePlannedAt;
            foreach (var visit in future) visit.StatusCode = "PAUSED";
        }
        else if (code == "RESUME")
        {
            if (contract.StatusCode != "PAUSED") throw Conflict("CONTRACT_STATE_INVALID", "일시정지 중인 구독만 재개할 수 있습니다.");
            contract.StatusCode = "ACTIVE"; contract.PauseStartedAt = null; contract.ResumePlannedAt = null;
            foreach (var visit in future.Where(item => item.StatusCode == "PAUSED")) visit.StatusCode = "SCHEDULED";
        }
        else if (code == "TERMINATE")
        {
            if (contract.StatusCode == "TERMINATED" || contract.TerminationRequestedAt.HasValue) throw Conflict("CONTRACT_STATE_INVALID", "이미 해지되었거나 처리 중인 구독입니다.");
            await terminationService.RequestTerminationAsync(contract, identity.UserId, Clean(input.Reason), input.IdempotencyKey, now, token);
        }
        else throw Bad("CONTRACT_ACTION_INVALID", "구독 처리유형을 확인해 주세요.");
        contract.UpdatedAt = now; contract.UpdatedByUserId = identity.UserId;
        Event(contract.SubscriptionRequestId, contract.Id, null, code == "PAUSE" ? "PAUSED" : code == "RESUME" ? "RESUMED" : "TERMINATION_REQUESTED",
            identity.UserId, input.IdempotencyKey, now);
        if (code == "TERMINATE") Audit(identity.UserId, "SUBSCRIPTION_TERMINATION_REQUESTED", "SUBSCRIPTION_CONTRACT", contract.PublicId, input.Reason, now);
        await db.SaveChangesAsync(token);
        return await Contract(contract.Id, identity.ProfileId, token);
    }

    public async Task<CustomerSubscriptionVisitListItem> Skip(Guid id, CustomerSkipVisitRequest input, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var row = await VisitQuery(identity.ProfileId, tracking: true).SingleOrDefaultAsync(value => value.visit.PublicId == id, token)
            ?? throw NotFound("SUBSCRIPTION_VISIT_NOT_FOUND", "구독 회차를 찾을 수 없습니다.");
        if (await db.SubscriptionEvents.AsNoTracking().AnyAsync(item => item.IdempotencyKey == input.IdempotencyKey, token)) return MapVisit(row);
        if (row.visit.ScheduledStartAt <= DateTime.UtcNow.AddHours(24) || row.visit.StatusCode is "COMPLETED" or "CANCELLED" or "SKIPPED")
            throw Conflict("VISIT_SKIP_CUTOFF", "방문 24시간 전까지만 회차를 건너뛸 수 있습니다. 이후에는 전문가와 일정 변경을 협의해 주세요.");
        ApplyVersion(row.visit, input.RowVersion);
        row.visit.StatusCode = "SKIPPED"; row.visit.UpdatedAt = DateTime.UtcNow; row.visit.UpdatedByUserId = identity.UserId;
        Event(row.contract.SubscriptionRequestId, row.contract.Id, row.visit.Id, "VISIT_SKIPPED", identity.UserId, input.IdempotencyKey, DateTime.UtcNow);
        await db.SaveChangesAsync(token);
        return MapVisit(row);
    }

    public async Task<SubscriptionScheduleChangeResponse> CancelScheduleChange(Guid id, CustomerCancelScheduleChangeRequest input, ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var row = await (from change in db.SubscriptionScheduleChanges
                          join visit in db.SubscriptionVisitSchedules on change.SubscriptionVisitScheduleId equals visit.Id
                          join contract in db.SubscriptionContracts on visit.SubscriptionContractId equals contract.Id
                          where change.PublicId == id && contract.CustomerProfileId == identity.ProfileId
                          select new { change, visit, contract }).SingleOrDefaultAsync(token) ?? throw NotFound("SCHEDULE_CHANGE_NOT_FOUND", "일정 변경 요청을 찾을 수 없습니다.");
        var item = row.change;
        if (await db.SubscriptionEvents.AsNoTracking().AnyAsync(value => value.IdempotencyKey == input.IdempotencyKey, token)) return MapChange(item);
        if (item.StatusCode != "REQUESTED") throw Conflict("SCHEDULE_CHANGE_ALREADY_DECIDED", "처리 전 요청만 취소할 수 있습니다.");
        var now = DateTime.UtcNow;
        ApplyVersion(item, input.RowVersion); item.StatusCode = "CANCELLED"; item.DecidedAt = now; item.DecidedByUserId = identity.UserId; item.UpdatedAt = now;
        Event(row.contract.SubscriptionRequestId, row.contract.Id, row.visit.Id, "SCHEDULE_CHANGE_CANCELLED", identity.UserId, input.IdempotencyKey, now);
        Audit(identity.UserId, "SCHEDULE_CHANGE_CANCELLED", "SUBSCRIPTION_SCHEDULE_CHANGE", item.PublicId, null, now);
        await db.SaveChangesAsync(token); return MapChange(item);
    }

    public async Task<IReadOnlyList<CustomerSubscriptionPaymentMethodResponse>> PaymentMethods(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        return await db.SubscriptionPaymentMethods.AsNoTracking().Where(item => item.CustomerProfileId == identity.ProfileId)
            .OrderByDescending(item => item.IsDefault).ThenByDescending(item => item.RegisteredAt)
            .Select(item => new CustomerSubscriptionPaymentMethodResponse(item.PublicId, item.PaymentMethodTypeCode, item.ProviderCode,
                item.MaskedDisplayText, item.StatusCode, item.IsDefault, item.RegisteredAt)).ToListAsync(token);
    }

    public async Task<SubscriptionBillingRegistrationResponse> BillingRegistration(ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);var customer=await db.CustomerProfiles.AsNoTracking().SingleAsync(x=>x.Id==identity.ProfileId,token);var settings=paymentOptions.Value;
        return new(settings.Enabled&&!string.IsNullOrWhiteSpace(settings.ClientKey)&&!string.IsNullOrWhiteSpace(settings.SecretKey),settings.ProviderCode,settings.ClientKey,SubscriptionPaymentIdentity.CustomerKey(customer.PublicId),settings.BillingSuccessPath,settings.BillingFailPath);
    }

    public async Task<CustomerSubscriptionPaymentMethodResponse> CompleteBillingAuthorization(CompleteBillingAuthorizationRequest input,ClaimsPrincipal principal,CancellationToken token)
    {
        var identity=await Customer(principal,token);var customer=await db.CustomerProfiles.SingleAsync(x=>x.Id==identity.ProfileId,token);var expected=SubscriptionPaymentIdentity.CustomerKey(customer.PublicId);if(!string.Equals(expected,input.CustomerKey,StringComparison.Ordinal))throw Bad("SUBSCRIPTION_CUSTOMER_KEY_INVALID","결제수단 인증 고객정보가 일치하지 않습니다.");
        var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.AuthKey)));var issued=await paymentGateway.IssueBillingKeyAsync(paymentOptions.Value.ProviderCode,input.AuthKey,expected,$"billing-authorization:{digest}",token);var now=DateTime.UtcNow;
        if(input.IsDefault)foreach(var old in await db.SubscriptionPaymentMethods.Where(x=>x.CustomerProfileId==customer.Id&&x.IsDefault).ToListAsync(token))old.IsDefault=false;
        var method=new SubscriptionPaymentMethod{CustomerProfileId=customer.Id,ProviderCode=paymentOptions.Value.ProviderCode,PaymentMethodTypeCode=issued.MethodType,ExternalTokenReference=tokenProtector.Protect(issued.BillingKey),MaskedDisplayText=issued.MaskedDisplayText,StatusCode="ACTIVE",IsDefault=input.IsDefault,RegisteredAt=now,CreatedAt=now,CreatedByUserId=identity.UserId,UpdatedAt=now,UpdatedByUserId=identity.UserId};db.SubscriptionPaymentMethods.Add(method);Audit(identity.UserId,"SUBSCRIPTION_PAYMENT_METHOD_REGISTERED","SUBSCRIPTION_PAYMENT_METHOD",method.PublicId,null,now);await db.SaveChangesAsync(token);return new(method.PublicId,method.PaymentMethodTypeCode,method.ProviderCode,method.MaskedDisplayText,method.StatusCode,method.IsDefault,method.RegisteredAt);
    }

    public async Task<CustomerSubscriptionContractResponse> ConsentRecurringPayment(Guid id, CustomerRecurringPaymentConsentRequest input, ClaimsPrincipal principal, CancellationToken token)
    {
        if (!input.Consent) throw Bad("SUBSCRIPTION_RECURRING_CONSENT_REQUIRED", "정기결제 동의가 필요합니다.");
        var identity = await Customer(principal, token);
        var contract = await db.SubscriptionContracts.SingleOrDefaultAsync(item => item.PublicId == id && item.CustomerProfileId == identity.ProfileId, token)
            ?? throw NotFound("SUBSCRIPTION_CONTRACT_NOT_FOUND", "구독 계약을 찾을 수 없습니다.");
        if (contract.StatusCode != "PAYMENT_PENDING") throw Conflict("SUBSCRIPTION_BILLING_STATE_INVALID", "첫 결제 대기 계약에서만 정기결제를 설정할 수 있습니다.");
        if (await db.SubscriptionEvents.AsNoTracking().AnyAsync(item => item.IdempotencyKey == input.IdempotencyKey, token)) return await Contract(contract.Id, identity.ProfileId, token);
        ApplyVersion(contract, input.RowVersion);
        var method = await db.SubscriptionPaymentMethods.SingleOrDefaultAsync(item => item.PublicId == input.PaymentMethodId && item.CustomerProfileId == identity.ProfileId && item.StatusCode == "ACTIVE", token)
            ?? throw NotFound("SUBSCRIPTION_PAYMENT_METHOD_NOT_FOUND", "사용 가능한 결제수단을 찾을 수 없습니다.");
        var payment = await db.SubscriptionPaymentRequests.Where(item => item.SubscriptionContractId == contract.Id && item.StatusCode == "REQUESTED").OrderBy(item => item.RequestedAt).FirstOrDefaultAsync(token)
            ?? throw NotFound("SUBSCRIPTION_INITIAL_PAYMENT_NOT_FOUND", "첫 결제 요청을 찾을 수 없습니다.");
        var now = DateTime.UtcNow; payment.PaymentMethodId = method.Id; contract.PaymentMethodId = method.Id; payment.UpdatedAt = now; payment.UpdatedByUserId = identity.UserId; contract.BillingStatusCode = "AUTO_PAY_CONSENTED"; contract.UpdatedAt = now; contract.UpdatedByUserId = identity.UserId;
        Event(contract.SubscriptionRequestId, contract.Id, null, "RECURRING_PAYMENT_CONSENTED", identity.UserId, input.IdempotencyKey, now);
        Audit(identity.UserId, "SUBSCRIPTION_RECURRING_PAYMENT_CONSENTED", "SUBSCRIPTION_CONTRACT", contract.PublicId, null, now);
        await db.SaveChangesAsync(token);await paymentProcessor.ChargeAsync(payment.PublicId,token);return await Contract(contract.Id, identity.ProfileId, token);
    }

    public async Task<IReadOnlyList<CustomerSubscriptionPaymentHistoryResponse>> Payments(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Customer(principal, token);
        var rows = await (from payment in db.SubscriptionPaymentRequests.AsNoTracking()
                          join contract in db.SubscriptionContracts.AsNoTracking() on payment.SubscriptionContractId equals contract.Id
                          join service in db.ServiceCategories.AsNoTracking() on contract.ServiceCategoryId equals service.Id
                          where payment.CustomerProfileId == identity.ProfileId &&
                                (payment.StatusCode != "REQUESTED" || payment.PaymentMethodId != null)
                          orderby payment.RequestedAt descending
                          select new { payment, contract, service }).ToListAsync(token);
        var result = new List<CustomerSubscriptionPaymentHistoryResponse>();
        foreach (var row in rows)
        {
            var refunds = await db.SubscriptionRefundAdjustments.AsNoTracking().Where(item => item.PaymentRequestId == row.payment.Id)
                .OrderByDescending(item => item.RequestedAt).Select(item => new CustomerSubscriptionRefundResponse(item.PublicId, item.TypeCode,
                    item.RequestedAmount, item.ApprovedAmount, item.StatusCode, item.RequestedAt, item.CompletedAt)).ToListAsync(token);
            result.Add(new CustomerSubscriptionPaymentHistoryResponse(row.payment.PublicId, row.contract.PublicId, Number("SC", row.contract.PublicId),
                row.service.Name, row.payment.BillingPeriodStart, row.payment.BillingPeriodEnd, row.payment.RequestedAmount, row.payment.CurrencyCode,
                row.payment.StatusCode, row.payment.RequestedAt, row.payment.CompletedAt ?? row.payment.FailedAt ?? row.payment.CancelledAt,
                row.payment.FailureReason, refunds));
        }
        return result;
    }

    private async Task<CustomerSubscriptionRequestResponse> Request(long id, long customerId, CancellationToken token)
    {
        var row = await (from item in db.SubscriptionRequests.AsNoTracking()
                         join category in db.ServiceCategories.AsNoTracking() on item.ServiceCategoryId equals category.Id
                         join area in db.AdministrativeAreas.AsNoTracking() on item.AdministrativeAreaId equals area.Id
                         join rule in db.SubscriptionRecurrenceRules.AsNoTracking() on item.Id equals rule.SubscriptionRequestId
                         where item.Id == id && item.CustomerProfileId == customerId
                         select new { item, category, area, rule }).SingleOrDefaultAsync(token)
            ?? throw NotFound("SUBSCRIPTION_REQUEST_NOT_FOUND", "구독 요청을 찾을 수 없습니다.");
        var product = row.item.CareProductId.HasValue ? await db.CareProducts.AsNoTracking().Where(item => item.Id == row.item.CareProductId)
            .Select(item => new { item.PublicId, item.ProductName }).SingleAsync(token) : null;
        var parentAreaName = row.area.ParentAreaId.HasValue
            ? await db.AdministrativeAreas.AsNoTracking().Where(item => item.Id == row.area.ParentAreaId.Value)
                .Select(item => item.AreaName).SingleOrDefaultAsync(token)
            : null;
        var displayAreaName = string.IsNullOrWhiteSpace(parentAreaName) || parentAreaName == row.area.AreaName
            ? row.area.AreaName
            : $"{parentAreaName} / {row.area.AreaName}";
        var preference = CareSubscriptionService.PricePreference(row.rule);
        return new CustomerSubscriptionRequestResponse(row.item.PublicId, Number("SR", row.item.PublicId), row.category.PublicId, row.category.Name,
            product?.PublicId, product?.ProductName, row.item.RequestTypeCode, row.item.RequestedScopeText, row.item.PreferredStartDate, row.area.PublicId,
            displayAreaName, row.item.DetailAddress, row.item.StatusCode,
            await db.SubscriptionApplications.CountAsync(item => item.SubscriptionRequestId == row.item.Id, token), row.item.SelectedApplicationId.HasValue,
            preference.PriceNegotiable, preference.DesiredMonthlyAmount, preference.DesiredVisitAmount, MapRule(row.rule), row.item.CreatedAt, Version(row.item.RowVersion));
    }

    private async Task<CustomerSubscriptionApplicationResponse> Application(SubscriptionApplication item, SubscriptionRequest request, CancellationToken token)
    {
        var provider = await db.ProviderProfiles.AsNoTracking().SingleAsync(value => value.Id == item.ProviderProfileId, token);
        var service = await db.ProviderServiceCategories.AsNoTracking().SingleOrDefaultAsync(value => value.ProviderProfileId == provider.Id && value.CategoryId == request.ServiceCategoryId && value.StatusCode == "ACTIVE", token);
        var approval = service is null ? "NOT_REGISTERED" : await db.ProviderServiceApprovals.AsNoTracking().Where(value => value.ProviderServiceCategoryId == service.Id)
            .Select(value => value.ApprovalStatusCode).SingleOrDefaultAsync(token) ?? "PENDING";
        var trust = await db.ProviderTrustScoreCurrent.AsNoTracking().SingleOrDefaultAsync(value => value.ProviderProfileId == provider.Id, token);
        var reviewIds = await db.Reviews.AsNoTracking().Where(value => value.ProviderProfileId == provider.Id && value.VisibilityStatusCode == "PUBLIC" &&
            (value.VerificationStatusCode == "VERIFIED_TRANSACTION" || value.VerificationStatusCode == "VERIFIED_SUBSCRIPTION_VISIT"))
            .Select(value => value.Id).ToArrayAsync(token);
        var averages = await (from rating in db.ReviewRatings.AsNoTracking()
                              join definition in db.ReviewRatingItems.AsNoTracking() on rating.RatingItemId equals definition.Id
                              where reviewIds.Contains(rating.ReviewId) && definition.IsActive
                              group rating by new { definition.PublicId, definition.Code, definition.Name, definition.MinValue, definition.MaxValue } into values
                              orderby values.Key.Name
                              select new CustomerSubscriptionRatingAverage(values.Key.PublicId, values.Key.Code, values.Key.Name,
                                  values.Average(value => value.RatingValue), values.Count(), values.Key.MinValue, values.Key.MaxValue)).ToListAsync(token);
        var requirement = await RequirementSummary(provider.Id, service?.Id, request.ServiceCategoryId, token);
        var score = trust?.EvaluationStatusCode == "CALCULATED" ? trust.Score : null;
        return new CustomerSubscriptionApplicationResponse(item.PublicId, provider.PublicId, provider.BusinessName, item.ProposedScopeText,
            item.ProposedMonthlyAmount, item.ProposedVisitAmount, item.AvailableScheduleText, score, score.HasValue ? trust?.GradeCode : null,
            trust?.EvaluationStatusCode ?? "NEW_OR_EVALUATING", score.HasValue ? $"{score:0.##}점{(string.IsNullOrWhiteSpace(trust?.GradeCode) ? "" : $" · {trust.GradeCode}")}" : "신규·평가중",
            reviewIds.Length, averages, provider.ApprovalStatusCode == "APPROVED" ? "본사 전문가 승인 완료" : "전문가 승인 확인 필요",
            approval == "APPROVED" ? "해당 서비스 승인 완료" : "서비스 승인 확인 필요", requirement, item.StatusCode, item.SubmittedAt,
            request.SelectedApplicationId == item.Id, Version(item.RowVersion));
    }

    private async Task<string> RequirementSummary(long providerId, long? providerServiceId, long categoryId, CancellationToken token)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var policyIds = await db.CategoryOperationPolicies.AsNoTracking().Where(item => item.CategoryId == categoryId && item.IsActive &&
            item.EffectiveFrom <= today && (item.EffectiveTo == null || item.EffectiveTo > today)).Select(item => item.Id).ToArrayAsync(token);
        var required = await db.CategoryProviderRequirementAssignments.AsNoTracking().Where(item => policyIds.Contains(item.CategoryOperationPolicyId) && item.IsActive && item.IsRequired).ToListAsync(token);
        if (required.Count == 0) return "구조화 요건 미설정";
        if (!providerServiceId.HasValue) return "필수 요건 확인 필요";
        var ids = required.Select(item => item.Id).ToArray();
        var approved = await db.ProviderServiceRequirementVerifications.AsNoTracking().CountAsync(item => item.ProviderServiceCategoryId == providerServiceId && ids.Contains(item.RequirementAssignmentId) && item.VerificationStatusCode == "APPROVED", token);
        return approved == required.Count ? $"필수 요건 {approved}건 확인" : $"필수 요건 {approved}/{required.Count}건 확인";
    }

    private async Task<CustomerSubscriptionContractResponse> Contract(long id, long customerId, CancellationToken token)
    {
        var row = await (from item in db.SubscriptionContracts.AsNoTracking()
                         join request in db.SubscriptionRequests.AsNoTracking() on item.SubscriptionRequestId equals request.Id
                         join provider in db.ProviderProfiles.AsNoTracking() on item.ProviderProfileId equals provider.Id
                         join category in db.ServiceCategories.AsNoTracking() on item.ServiceCategoryId equals category.Id
                         join application in db.SubscriptionApplications.AsNoTracking() on item.SubscriptionApplicationId equals application.Id
                         join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                         join rule in db.SubscriptionRecurrenceRules.AsNoTracking() on request.Id equals rule.SubscriptionRequestId
                         where item.Id == id && item.CustomerProfileId == customerId
                         select new { item, request, provider, category, application, area, rule }).SingleOrDefaultAsync(token)
            ?? throw NotFound("SUBSCRIPTION_CONTRACT_NOT_FOUND", "구독 계약을 찾을 수 없습니다.");
        var product = row.item.CareProductId.HasValue ? await db.CareProducts.AsNoTracking().Where(value => value.Id == row.item.CareProductId)
            .Select(value => new { value.ProductName, value.StandardMonthlyAmount, value.StandardVisitAmount }).SingleAsync(token) : null;
        var next = await db.SubscriptionVisitSchedules.AsNoTracking().Where(value => value.SubscriptionContractId == row.item.Id && value.ScheduledStartAt >= DateTime.UtcNow && value.StatusCode != "CANCELLED" && value.StatusCode != "SKIPPED")
            .OrderBy(value => value.ScheduledStartAt).Select(value => (DateTime?)value.ScheduledStartAt).FirstOrDefaultAsync(token);
        var parentAreaName = row.area.ParentAreaId.HasValue
            ? await db.AdministrativeAreas.AsNoTracking().Where(value => value.Id == row.area.ParentAreaId.Value).Select(value => value.AreaName).SingleOrDefaultAsync(token)
            : null;
        var requestAreaName = string.IsNullOrWhiteSpace(parentAreaName) || parentAreaName == row.area.AreaName ? row.area.AreaName : $"{parentAreaName} / {row.area.AreaName}";
        var requestPrice = CareSubscriptionService.PricePreference(row.rule);
        return new CustomerSubscriptionContractResponse(row.item.PublicId, Number("SC", row.item.PublicId), row.request.PublicId, row.category.Name,
            product?.ProductName, requestAreaName, row.request.RequestedScopeText, row.request.PreferredStartDate, requestPrice.PriceNegotiable,
            requestPrice.DesiredMonthlyAmount, requestPrice.DesiredVisitAmount, row.provider.PublicId, row.provider.BusinessName, row.item.StatusCode,
            row.item.TerminationRequestedAt.HasValue && !row.item.TerminatedAt.HasValue ? "해지 처리 대기" : ContractStatus(row.item.StatusCode), row.item.StartedAt,
            row.item.EndedAt, row.item.PauseStartedAt, row.item.ResumePlannedAt, row.item.TerminationRequestedAt, row.item.TerminatedAt,
            row.application.ProposedMonthlyAmount ?? product?.StandardMonthlyAmount, row.application.ProposedVisitAmount ?? product?.StandardVisitAmount,
            row.item.CurrencyCode, next, row.item.PriceSnapshotJson, row.item.ServiceScopeSnapshotJson, row.item.RecurrenceSnapshotJson,
            row.item.ProviderTrustScoreSnapshot, true, row.item.NextBillingAt, row.item.BillingStatusCode, Version(row.item.RowVersion));
    }

    private IQueryable<VisitRow> VisitQuery(long customerId, bool tracking = false)
    {
        var visits = tracking ? db.SubscriptionVisitSchedules : db.SubscriptionVisitSchedules.AsNoTracking();
        var contracts = tracking ? db.SubscriptionContracts : db.SubscriptionContracts.AsNoTracking();
        return from visit in visits
               join contract in contracts on visit.SubscriptionContractId equals contract.Id
               join provider in db.ProviderProfiles.AsNoTracking() on visit.ProviderProfileId equals provider.Id
               join service in db.ServiceCategories.AsNoTracking() on contract.ServiceCategoryId equals service.Id
               where contract.CustomerProfileId == customerId
               select new VisitRow { visit = visit, contract = contract, provider = provider, service = service };
    }

    private static CustomerSubscriptionVisitListItem MapVisit(VisitRow row) => new(row.visit.PublicId, row.contract.PublicId,
        Number("SC", row.contract.PublicId), row.service.Name, row.visit.VisitNo, row.provider.PublicId, row.provider.BusinessName,
        row.visit.ScheduledStartAt, row.visit.ScheduledEndAt, row.visit.StatusCode, VisitStatus(row.visit.StatusCode), row.visit.VisitVerifiedAt.HasValue,
        row.visit.ProviderCompletionSubmittedAt.HasValue, row.visit.CustomerConfirmedAt.HasValue);

    private SubscriptionScheduleChangeResponse MapChange(SubscriptionScheduleChange item)
    {
        var old = System.Text.Json.JsonSerializer.Deserialize<ScheduleValue>(item.OldScheduleJson)!;
        var next = System.Text.Json.JsonSerializer.Deserialize<ScheduleValue>(item.NewScheduleJson)!;
        var visitId = db.SubscriptionVisitSchedules.Where(value => value.Id == item.SubscriptionVisitScheduleId).Select(value => value.PublicId).Single();
        return new SubscriptionScheduleChangeResponse(item.PublicId, visitId, old.start, old.end, next.start, next.end, item.Reason,
            item.StatusCode, item.RequestedAt, item.DecidedAt, Version(item.RowVersion));
    }

    private async Task<(long UserId, long ProfileId)> Customer(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId)) throw Forbidden("AUTHENTICATED_USER_REQUIRED", "로그인이 필요합니다.");
        return await (from user in db.Users.AsNoTracking()
                      join profile in db.CustomerProfiles.AsNoTracking() on user.Id equals profile.UserId
                      where user.PublicId == publicId && user.StatusCode == "ACTIVE"
                      select new ValueTuple<long, long>(user.Id, profile.Id)).SingleOrDefaultAsync(token) is var value && value.Item1 != 0
            ? value : throw Forbidden("CUSTOMER_PROFILE_REQUIRED", "고객 프로필이 필요합니다.");
    }

    private void Event(long? requestId, long? contractId, long? visitId, string type, long actor, string key, DateTime now) =>
        db.SubscriptionEvents.Add(new SubscriptionEvent { SubscriptionRequestId = requestId, SubscriptionContractId = contractId,
            SubscriptionVisitScheduleId = visitId, EventTypeCode = type, OccurredAt = now, ActorUserId = actor, IdempotencyKey = key.Trim() });
    private void Audit(long actor, string action, string entity, Guid id, string? reason, DateTime now) =>
        db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = actor, ActorRoleCode = RoleCodes.Customer,
            ActionCode = action, EntityType = entity, EntityPublicId = id, ResultCode = "SUCCESS", Reason = Clean(reason) });
    private void ApplyVersion(object entity, string? value) { if (!string.IsNullOrWhiteSpace(value)) db.Entry(entity).Property("RowVersion").OriginalValue = Convert.FromBase64String(value); }
    private static SubscriptionRecurrenceResponse MapRule(SubscriptionRecurrenceRule item) => new(item.FrequencyTypeCode, item.IntervalValue,
        item.VisitsPerPeriod, string.IsNullOrWhiteSpace(item.WeekdaysJson) ? [] : System.Text.Json.JsonSerializer.Deserialize<List<int>>(item.WeekdaysJson) ?? [],
        item.PreferredTimeFrom, item.PreferredTimeTo, item.ExpectedDurationMinutes, item.StartDate, item.EndDate);
    private static string CustomerProgress(SubscriptionVisitSchedule visit) => visit.StatusCode switch { "PROVIDER_COMPLETED" => "완료보고 확인 대기", "COMPLETED" => "작업 확인 완료", "DISPUTED" => "확인 중", "SKIPPED" => "이번 회차 건너뜀", "CANCELLED" => "취소된 회차", _ => VisitStatus(visit.StatusCode) };
    private static string ContractStatus(string code) => code switch { "PAYMENT_PENDING" => "첫 결제 대기", "ACTIVE" => "이용 중", "PAUSED" => "일시정지", "TERMINATION_REQUESTED" => "해지 처리 대기", "TERMINATED" => "해지", "COMPLETED" => "종료", _ => "상태 확인 중" };
    private static string VisitStatus(string code) => code switch { "SCHEDULED" => "방문 예정", "RESCHEDULED" => "변경 일정 확정", "PAUSED" => "일시정지", "PROVIDER_COMPLETED" => "완료보고 도착", "COMPLETED" => "작업 확인 완료", "SKIPPED" => "건너뜀", "CANCELLED" => "취소", "DISPUTED" => "확인 중", _ => "상태 확인 중" };
    private static string Number(string prefix, Guid id) => $"{prefix}-{id:N}"[..Math.Min(prefix.Length + 13, prefix.Length + 33)].ToUpperInvariant();
    private static string Version(byte[] value) => value.Length == 0 ? string.Empty : Convert.ToBase64String(value);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static SubscriptionBusinessException Bad(string code, string message) => new(400, code, message);
    private static SubscriptionBusinessException Forbidden(string code, string message) => new(403, code, message);
    private static SubscriptionBusinessException NotFound(string code, string message) => new(404, code, message);
    private static SubscriptionBusinessException Conflict(string code, string message) => new(409, code, message);
    private sealed class VisitRow
    {
        public required SubscriptionVisitSchedule visit { get; init; }
        public required SubscriptionContract contract { get; init; }
        public required ProviderProfile provider { get; init; }
        public required ServiceCategory service { get; init; }
    }
    private sealed record ScheduleValue(DateTime start, DateTime? end);
}
