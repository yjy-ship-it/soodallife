using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class TrustCalculationEngineTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]public async Task DraftV1_IsSeededWithSixComponentsAndWeight100(){using var client=Client();await Login(client,RoleCodes.Admin);var values=await client.GetFromJsonAsync<List<TrustPolicyListItem>>("/api/v1/admin/trust/policies");var policy=Assert.Single(values!,x=>x.PolicyVersion==TrustPolicyDraftDefaults.Version);Assert.Equal("DRAFT",policy.StatusCode);Assert.Equal(100,policy.TotalWeight);Assert.Equal(3,policy.MinimumCompletedTransactions);Assert.Equal(3,policy.MinimumVerifiedReviews);Assert.Equal(6,policy.Components.Count);}

    [Fact]public async Task InvalidWeight_IsRejected(){using var client=Client();await Login(client,RoleCodes.Admin);var policy=await Draft(client);var input=ToRequest(policy,policy.Components.Select((x,i)=>new{x.Code,Weight=i==0?14m:x.Weight,x.RuleType,x.SettingsJson}));var response=await client.PutAsJsonAsync($"/api/v1/admin/trust/policies/{policy.Id}",input);Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);}

    [Fact]public async Task MissingRequiredRuleSettings_AreRejected(){using var client=Client();await Login(client,RoleCodes.Admin);var policy=await Draft(client);var input=ToRequest(policy,policy.Components.Select(x=>new{x.Code,x.Weight,x.RuleType,SettingsJson=x.Code=="EVIDENCE"?"{}":x.SettingsJson}));var response=await client.PutAsJsonAsync($"/api/v1/admin/trust/policies/{policy.Id}",input);Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);}

    [Fact]public async Task DraftSimulation_PreservesCurrentLegacyAndEvents_AndUsesCleanHistoryBaselines(){Guid providerId;decimal? profileBefore,currentBefore;int eventsBefore;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var provider=await db.ProviderProfiles.FirstAsync();providerId=provider.PublicId;profileBefore=provider.TrustScore;currentBefore=await db.ProviderTrustScoreCurrent.Where(x=>x.ProviderProfileId==provider.Id).Select(x=>x.Score).SingleOrDefaultAsync();eventsBefore=await db.TrustScoreEvents.CountAsync();}using var client=Client();await Login(client,RoleCodes.Admin);var policy=await Draft(client);var response=await client.PostAsJsonAsync($"/api/v1/admin/trust/{providerId}/simulation",new{policyId=policy.Id,idempotencyKey=$"simulation-{Guid.NewGuid():N}"});response.EnsureSuccessStatusCode();var result=await response.Content.ReadFromJsonAsync<TrustCalculationResponse>();Assert.NotNull(result);Assert.Equal("SIMULATION",result.CalculationModeCode);Assert.Equal("NEW_OR_EVALUATING",result.EvaluationStatusCode);Assert.Null(result.Score);Assert.Equal(6,result.Components.Count);Assert.Contains(result.Components,x=>x.Code=="REVIEW"&&!x.IsCalculable);Assert.Contains(result.Components,x=>x.Code=="AFTER_SERVICE"&&x.IsCalculable&&x.NormalizedScore==100);Assert.Contains(result.Components,x=>x.Code=="DISPUTE"&&x.IsCalculable&&x.NormalizedScore==100);Assert.Contains(result.Components,x=>x.Code=="SANCTION"&&x.IsCalculable&&x.NormalizedScore==100);using var verify=factory.Services.CreateScope();var check=verify.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var providerAfter=await check.ProviderProfiles.SingleAsync(x=>x.PublicId==providerId);Assert.Equal(profileBefore,providerAfter.TrustScore);Assert.Equal(currentBefore,await check.ProviderTrustScoreCurrent.Where(x=>x.ProviderProfileId==providerAfter.Id).Select(x=>x.Score).SingleOrDefaultAsync());Assert.Equal(eventsBefore,await check.TrustScoreEvents.CountAsync());}

    [Fact]public async Task Simulation_IsIdempotent(){Guid providerId;using(var scope=factory.Services.CreateScope()){providerId=(await scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>().ProviderProfiles.FirstAsync()).PublicId;}using var client=Client();await Login(client,RoleCodes.Admin);var policy=await Draft(client);var key=$"simulation-idempotent-{Guid.NewGuid():N}";var first=await(await client.PostAsJsonAsync($"/api/v1/admin/trust/{providerId}/simulation",new{policyId=policy.Id,idempotencyKey=key})).Content.ReadFromJsonAsync<TrustCalculationResponse>();var second=await(await client.PostAsJsonAsync($"/api/v1/admin/trust/{providerId}/simulation",new{policyId=policy.Id,idempotencyKey=key})).Content.ReadFromJsonAsync<TrustCalculationResponse>();Assert.Equal(first!.ResultId,second!.ResultId);}

    [Fact]public async Task ActivePolicy_CannotBeEdited_AndCanBeRetired(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var policy=await db.TrustPolicies.SingleAsync(x=>x.PolicyVersion==TrustPolicyDraftDefaults.Version);policy.StatusCode="ACTIVE";await db.SaveChangesAsync();using var client=Client();await Login(client,RoleCodes.Admin);var current=(await client.GetFromJsonAsync<List<TrustPolicyListItem>>("/api/v1/admin/trust/policies"))!.Single(x=>x.Id==policy.PublicId);var response=await client.PutAsJsonAsync($"/api/v1/admin/trust/policies/{current.Id}",ToRequest(current,current.Components.Select(x=>new{x.Code,x.Weight,x.RuleType,x.SettingsJson})));Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);var actor=(await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Admin].LoginId)).PublicId;var retired=await scope.ServiceProvider.GetRequiredService<TrustCalculationService>().RetirePolicyAsync(policy.PublicId,new("운영 종료 검증",$"retire-{Guid.NewGuid():N}",Convert.ToBase64String(policy.RowVersion)),actor,default);Assert.Equal("RETIRED",retired.StatusCode);policy.StatusCode="DRAFT";await db.SaveChangesAsync();}

    [Fact]public async Task ApprovedPolicyPeriodOverlap_IsRejected(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var draft=await db.TrustPolicies.SingleAsync(x=>x.PolicyVersion==TrustPolicyDraftDefaults.Version);var now=DateTime.UtcNow;var blocker=new TrustPolicy{PolicyVersion=$"approved-{Guid.NewGuid():N}",PolicyName="기간 중복 검증 정책",TargetTypeCode="PROVIDER",ScopeTypeCode="GLOBAL",StatusCode="APPROVED",RulesJson=draft.RulesJson,EffectiveFrom=draft.EffectiveFrom,CreatedAt=now,UpdatedAt=now};db.TrustPolicies.Add(blocker);await db.SaveChangesAsync();var admin=(await db.Users.SingleAsync(x=>x.LoginId==factory.Credentials[RoleCodes.Admin].LoginId)).PublicId;var service=scope.ServiceProvider.GetRequiredService<TrustCalculationService>();await Assert.ThrowsAsync<TrustCalculationException>(()=>service.ApprovePolicyAsync(draft.PublicId,new("기간 중복 검증",$"approve-{Guid.NewGuid():N}",Convert.ToBase64String(draft.RowVersion)),admin,default));db.TrustPolicies.Remove(blocker);await db.SaveChangesAsync();}

    [Fact]public async Task ActualCalculation_RequiresActivePolicy_AndDoesNotMutateOnDraft(){Guid providerId;int events;using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var active=await db.TrustPolicies.Where(x=>x.StatusCode=="ACTIVE").ToListAsync();foreach(var policy in active)policy.StatusCode="RETIRED";await db.SaveChangesAsync();providerId=(await db.ProviderProfiles.FirstAsync()).PublicId;events=await db.TrustScoreEvents.CountAsync();var service=scope.ServiceProvider.GetRequiredService<TrustCalculationService>();var error=await Assert.ThrowsAsync<TrustCalculationException>(()=>service.CalculateActiveAsync(providerId,"TEST",null,$"actual-{Guid.NewGuid():N}",null,default));Assert.Equal("ACTIVE_TRUST_POLICY_NOT_FOUND",error.BusinessCode);Assert.Equal(events,await db.TrustScoreEvents.CountAsync());}

    [Fact]
    public async Task ActiveCalculation_WritesOneEventAndCurrentTogether_AndIsIdempotent()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var now = DateTime.UtcNow;
        var user = new User { LoginId = $"trust-engine-provider-{Guid.NewGuid():N}", NormalizedLoginId = $"TRUST-{Guid.NewGuid():N}", StatusCode = "ACTIVE" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var provider = new ProviderProfile { UserId = user.Id, BusinessName = "신뢰도 산정 검증 전문가", ApprovalStatusCode = "APPROVED", ActivityStatusCode = "ACTIVE" };
        db.ProviderProfiles.Add(provider);
        await db.SaveChangesAsync();

        var assignment = await db.CategoryProviderRequirementAssignments.FirstAsync(x => x.IsActive && x.IsRequired);
        var assignmentCategoryId = await db.CategoryOperationPolicies.Where(x => x.Id == assignment.CategoryOperationPolicyId).Select(x => x.CategoryId).SingleAsync();
        var serviceCategory = await db.ServiceCategories.SingleAsync(x => x.Id == assignmentCategoryId);
        var providerService = new ProviderServiceCategory { ProviderProfileId = provider.Id, CategoryId = serviceCategory.Id, StatusCode = "ACTIVE", ActivatedAt = now };
        db.ProviderServiceCategories.Add(providerService);
        await db.SaveChangesAsync();
        db.ProviderServiceApprovals.Add(new ProviderServiceApproval { ProviderServiceCategoryId = providerService.Id, ApprovalStatusCode = "APPROVED", ApprovalRequestedAt = now, ApprovalDecidedAt = now, CreatedAt = now, UpdatedAt = now });
        db.ProviderServiceRequirementVerifications.Add(new ProviderServiceRequirementVerification { ProviderServiceCategoryId = providerService.Id, RequirementAssignmentId = assignment.Id, VerificationStatusCode = "APPROVED", VerifiedAt = now, CreatedAt = now, UpdatedAt = now });

        var transaction = new TransactionRecord { ProviderProfileId = provider.Id, StatusCode = "COMPLETED", CurrencyCode = "KRW", QuoteSnapshotJson = "{}", CategoryPolicySnapshotJson = "{}", CompletionPolicySnapshotJson = "{}", CompletedAt = now, CreatedAt = now, UpdatedAt = now };
        db.Transactions.Add(transaction);
        var ratingItem = new ReviewRatingItem { Code = $"TRUST_TEST_{Guid.NewGuid():N}", Name = "서비스 만족도", MinValue = 1, MaxValue = 5, DisplayOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now };
        db.ReviewRatingItems.Add(ratingItem);
        await db.SaveChangesAsync();
        var customer = await db.CustomerProfiles.FirstAsync();
        var review = new Review { TransactionId = transaction.Id, CustomerProfileId = customer.Id, ProviderProfileId = provider.Id, BodyText = "친절하고 약속한 작업을 완료했습니다.", VerificationStatusCode = "VERIFIED_TRANSACTION", VisibilityStatusCode = "PUBLIC", IdempotencyKey = $"trust-review-{Guid.NewGuid():N}", SubmittedAt = now, PublishedAt = now, CreatedAt = now, UpdatedAt = now };
        db.Reviews.Add(review);
        await db.SaveChangesAsync();
        db.ReviewRatings.Add(new ReviewRating { ReviewId = review.Id, RatingItemId = ratingItem.Id, RatingValue = 4, DisplayOrder = 1, CreatedAt = now });
        db.AfterServiceCases.Add(new AfterServiceCase { TransactionId = transaction.Id, CustomerProfileId = customer.Id, ProviderProfileId = provider.Id, StatusCode = "RESOLVED", Subject = "작업 상태 확인", Description = "확인 요청", ReceivedAt = now, CompletedAt = now, ResolutionSummary = "정상 처리", RecurrenceOccurred = false, CreatedAt = now, UpdatedAt = now });

        var liability = new DisputeLiabilityType { Code = "PROVIDER", Name = "전문가 귀책", IsActive = true, DisplayOrder = 1, CreatedAt = now, UpdatedAt = now };
        var sanctionType = new SanctionType { Code = "WARNING", Name = "주의", IsActive = true, DisplayOrder = 1, CreatedAt = now, UpdatedAt = now };
        db.AddRange(liability, sanctionType);
        await db.SaveChangesAsync();
        var adminId = await (from admin in db.Users join link in db.UserRoles on admin.Id equals link.UserId join role in db.Roles on link.RoleId equals role.Id where role.Code == RoleCodes.Admin select admin.Id).FirstAsync();
        var dispute = new DisputeCase { TransactionId = transaction.Id, ApplicantUserId = customer.UserId, CounterpartyUserId = user.Id, Subject = "작업 범위 확인", Description = "구조화 귀책 판정 검증", StatusCode = "RESOLVED", ReceivedAt = now, ResolvedAt = now, CreatedAt = now, UpdatedAt = now };
        db.DisputeCases.Add(dispute);
        await db.SaveChangesAsync();
        db.DisputeResolutions.Add(new DisputeResolution { DisputeCaseId = dispute.Id, LiabilityTypeId = liability.Id, VersionNo = 1, ResultSummary = "전문가 일부 귀책", DecisionDetails = "검증용 구조화 판정", BasisText = "확인 자료", DecidedAt = now, DecidedByUserId = adminId, IsCurrent = true, CreatedAt = now });

        var rules = TrustPolicyDraftDefaults.RulesJson
            .Replace("\"minimumCompletedTransactions\":3", "\"minimumCompletedTransactions\":1", StringComparison.Ordinal)
            .Replace("\"minimumVerifiedReviews\":3", "\"minimumVerifiedReviews\":1", StringComparison.Ordinal)
            .Replace("\"PROVIDER_FULL\":0", "\"PROVIDER_FULL\":0,\"PROVIDER\":80", StringComparison.Ordinal)
            .Replace("\"SUSPENSION\":0", "\"SUSPENSION\":0,\"WARNING\":80", StringComparison.Ordinal);
        var policy = new TrustPolicy { PolicyVersion = $"active-test-{Guid.NewGuid():N}", PolicyName = "자동산정 원자성 검증 정책", TargetTypeCode = "PROVIDER", ScopeTypeCode = "GLOBAL", StatusCode = "ACTIVE", RulesJson = rules, EffectiveFrom = now.AddMinutes(-1), CreatedAt = now, UpdatedAt = now };
        db.TrustPolicies.Add(policy);
        await db.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<TrustCalculationService>();
        var key = $"actual-success-{Guid.NewGuid():N}";
        var first = await service.CalculateActiveAsync(provider.PublicId, "TEST", transaction.PublicId, key, null, default);
        var second = await service.CalculateActiveAsync(provider.PublicId, "TEST", transaction.PublicId, key, null, default);
        var unchanged = await service.CalculateActiveAsync(provider.PublicId, "TEST", transaction.PublicId, $"unchanged-{Guid.NewGuid():N}", null, default);

        Assert.True(first.EvaluationStatusCode == "CALCULATED", first.InsufficiencyReason);
        Assert.NotNull(first.Score);
        Assert.Equal(first.ResultId, second.ResultId);
        Assert.NotEqual(first.ResultId, unchanged.ResultId);
        Assert.Equal(6, first.Components.Count);
        Assert.Equal(1, await db.TrustScoreEvents.CountAsync(x => x.ProviderProfileId == provider.Id));
        var current = await db.ProviderTrustScoreCurrent.SingleAsync(x => x.ProviderProfileId == provider.Id);
        Assert.Equal(first.Score, current.Score);
        Assert.NotNull(current.LastEventId);
        Assert.Null(provider.TrustScore);
        Assert.Null(transaction.ProviderTrustScoreSnapshot);

        policy.StatusCode = "RETIRED";
        ratingItem.IsActive = false;
        liability.IsActive = false;
        sanctionType.IsActive = false;
        await db.SaveChangesAsync();
    }

    [Theory,InlineData(RoleCodes.Customer),InlineData(RoleCodes.Provider)]public async Task NonAdmin_CannotAccessPolicyOrSimulation(string role){Guid providerId;using(var scope=factory.Services.CreateScope()){providerId=(await scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>().ProviderProfiles.FirstAsync()).PublicId;}using var client=Client();await Login(client,role);Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/admin/trust/policies")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync($"/api/v1/admin/trust/{providerId}/simulation",new{policyId=TrustPolicyDraftDefaults.PublicId,idempotencyKey=Guid.NewGuid().ToString("N")})).StatusCode);}

    [Fact]public async Task Admin_CanAccessPolicyAndSimulation(){using var client=Client();await Login(client,RoleCodes.Admin);Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/admin/trust/policies")).StatusCode);}

    private static object ToRequest(TrustPolicyListItem policy,IEnumerable<dynamic> components)=>new{policy.PolicyVersion,policy.PolicyName,policy.EffectiveFrom,policy.EffectiveTo,policy.MinimumCompletedTransactions,policy.MinimumVerifiedReviews,components=components.Select(x=>new{code=(string)x.Code,weight=(decimal)x.Weight,ruleType=(string)x.RuleType,settings=System.Text.Json.JsonSerializer.Deserialize<object>((string)x.SettingsJson)}),policy.RowVersion};
    private async Task<TrustPolicyListItem> Draft(HttpClient client)=>(await client.GetFromJsonAsync<List<TrustPolicyListItem>>("/api/v1/admin/trust/policies"))!.Single(x=>x.PolicyVersion==TrustPolicyDraftDefaults.Version);
    private HttpClient Client()=>factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});private Task<HttpResponseMessage> Login(HttpClient client,string role)=>client.PostAsJsonAsync("/api/v1/auth/login",new{LoginOrEmail=factory.Credentials[role].LoginId,factory.Credentials[role].Password});
}
