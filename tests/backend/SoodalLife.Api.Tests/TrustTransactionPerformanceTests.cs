using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Admin;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class TrustTransactionPerformanceTests(AuthenticationWebApplicationFactory factory):IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task CareVisitAndInteriorCompletion_AreIncludedOnceInTransactionPerformance()
    {
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();var now=DateTime.UtcNow;
        var provider=await db.ProviderProfiles.FirstAsync();var customer=await db.CustomerProfiles.FirstAsync();var category=await db.ServiceCategories.FirstAsync(x=>x.LevelCode=="SERVICE");var area=await db.AdministrativeAreas.FirstAsync(x=>x.AreaLevelCode=="SIGUNGU");var categoryPolicy=await db.CategoryPolicies.FirstAsync(x=>x.CategoryId==category.Id);
        var baselineTransactions=await db.Transactions.CountAsync(x=>x.ProviderProfileId==provider.Id&&x.StatusCode=="COMPLETED"&&!db.InteriorProjects.Any(project=>project.ServiceRequestId==x.ServiceRequestId));
        var baselineCare=await db.SubscriptionVisitSchedules.CountAsync(x=>x.ProviderProfileId==provider.Id&&x.StatusCode=="COMPLETED");
        var baselineInterior=await db.InteriorProjects.CountAsync(x=>x.SelectedContractorProviderId==provider.Id&&x.StatusCode=="COMPLETED");

        var careRequest=new SubscriptionRequest{CustomerProfileId=customer.Id,ServiceCategoryId=category.Id,AdministrativeAreaId=area.Id,RequestedScopeText="신뢰도 계산용 정기 방문",PreferredStartDate=DateOnly.FromDateTime(now),StatusCode="CONTRACTED",CreatedAt=now,UpdatedAt=now};db.SubscriptionRequests.Add(careRequest);await db.SaveChangesAsync();
        var application=new SubscriptionApplication{SubscriptionRequestId=careRequest.Id,ProviderProfileId=provider.Id,ProposedScopeText="정기 방문",StatusCode="SELECTED",SubmittedAt=now,IdempotencyKey=$"trust-care-{Guid.NewGuid():N}",CreatedAt=now,UpdatedAt=now};db.SubscriptionApplications.Add(application);await db.SaveChangesAsync();
        var contract=new SubscriptionContract{SubscriptionRequestId=careRequest.Id,CustomerProfileId=customer.Id,ProviderProfileId=provider.Id,ServiceCategoryId=category.Id,SubscriptionApplicationId=application.Id,StatusCode="ACTIVE",StartedAt=now,PriceSnapshotJson="{}",ServiceScopeSnapshotJson="{}",RecurrenceSnapshotJson="{}",CurrencyCode="KRW",CreatedAt=now,UpdatedAt=now};db.SubscriptionContracts.Add(contract);await db.SaveChangesAsync();
        db.SubscriptionVisitSchedules.Add(new(){SubscriptionContractId=contract.Id,VisitNo=1,ProviderProfileId=provider.Id,ScheduledStartAt=now,StatusCode="COMPLETED",ProviderCompletionSubmittedAt=now,CustomerConfirmedAt=now,SettlementStatusCode="READY",CreatedAt=now,UpdatedAt=now});

        var request=new ServiceRequest{CustomerProfileId=customer.Id,CategoryId=category.Id,CategoryPolicyId=categoryPolicy.Id,AdministrativeAreaId=area.Id,Title="신뢰도 계산용 인테리어",Description="완료 인테리어",StatusCode="COMPLETED",PolicySnapshotJson="{}",CreatedAt=now,UpdatedAt=now};db.ServiceRequests.Add(request);await db.SaveChangesAsync();
        db.InteriorProjects.Add(new(){ServiceRequestId=request.Id,CustomerProfileId=customer.Id,ServiceCategoryId=category.Id,SelectedContractorProviderId=provider.Id,StatusCode="COMPLETED",ActualCompletionDate=DateOnly.FromDateTime(now),CustomerCompletionAcknowledgedAt=now,CreatedAt=now,UpdatedAt=now});await db.SaveChangesAsync();

        var policy=await db.TrustPolicies.FirstAsync(x=>x.PolicyVersion==TrustPolicyDraftDefaults.Version);var adminPublicId=await(from user in db.Users join link in db.UserRoles on user.Id equals link.UserId join role in db.Roles on link.RoleId equals role.Id where role.Code=="ADMIN" select user.PublicId).FirstAsync();var result=await scope.ServiceProvider.GetRequiredService<TrustCalculationService>().SimulateAsync(provider.PublicId,policy.PublicId,$"unified-performance-{Guid.NewGuid():N}",adminPublicId,default);
        Assert.True(result.CompletedTransactionCount>=baselineTransactions+baselineCare+baselineInterior+2);
        var transaction=Assert.Single(result.Components,x=>x.Code=="TRANSACTION");Assert.Contains("CareVisitCompleted",transaction.RawValueJson);Assert.Contains("InteriorCompleted",transaction.RawValueJson);
    }
}
