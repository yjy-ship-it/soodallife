using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Automation;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ServiceRequestExpiryJobTests(AuthenticationWebApplicationFactory factory)
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task DueOpenRequest_ExpiresRequestDispatchAndCandidate_AndQueuesNotice()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var customerId = await (from user in db.Users join profile in db.CustomerProfiles on user.Id equals profile.UserId
                                where user.LoginId == factory.Credentials[RoleCodes.Customer].LoginId select profile.Id).SingleAsync();
        var providerId = await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId
                                where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select profile.Id).SingleAsync();
        var category = await db.ServiceCategories.SingleAsync(item => item.PublicId == factory.Catalog.ServiceId);
        var areaId = await db.AdministrativeAreas.Where(item => item.PublicId == factory.Catalog.AreaId).Select(item => item.Id).SingleAsync();
        var policyId = await db.CategoryPolicies.Where(item => item.CategoryId == category.Id).Select(item => item.Id).FirstAsync();
        var now = DateTime.UtcNow;
        var request = new ServiceRequest { CustomerProfileId = customerId, CategoryId = category.Id, CategoryPolicyId = policyId,
            AdministrativeAreaId = areaId, Title = "자동 만료 검증 요청", StatusCode = "OPEN", PolicySnapshotJson = "{}",
            OpenedAt = now.AddHours(-2), ExpiresAt = now.AddMinutes(-1), AbuseCountExcluded = true, CreatedAt = now.AddHours(-2), UpdatedAt = now.AddHours(-2) };
        db.ServiceRequests.Add(request); await db.SaveChangesAsync();
        var candidate = new DispatchCandidate { ServiceRequestId = request.Id, ProviderProfileId = providerId, StatusCode = "DISPATCHED",
            CategoryMatch = true, AreaMatch = true, ApprovalMatch = true, EvaluatedAt = now.AddHours(-1), ExpiresAt = now.AddMinutes(-1), CreatedAt = now.AddHours(-1) };
        db.DispatchCandidates.Add(candidate); await db.SaveChangesAsync();
        var dispatch = new RequestDispatch { ServiceRequestId = request.Id, ProviderProfileId = providerId, CandidateId = candidate.Id,
            StatusCode = "VIEWED", AvailableAt = now.AddHours(-1), ViewedAt = now.AddMinutes(-30), ExpiresAt = now.AddMinutes(-1),
            IdempotencyKey = $"expiry-{Guid.NewGuid():N}", CreatedAt = now.AddHours(-1) };
        db.RequestDispatches.Add(dispatch); await db.SaveChangesAsync();

        var result = await new ServiceRequestExpiryJob(db).ExecuteAsync(default);

        Assert.True(result.ProcessedCount >= 1);
        Assert.Equal("EXPIRED", request.StatusCode);
        Assert.Equal("EXPIRED", dispatch.StatusCode);
        Assert.Equal("EXPIRED", candidate.StatusCode);
        Assert.Equal("REQUEST_EXPIRED", candidate.ReasonCode);
        Assert.True(await db.OutboxEvents.AnyAsync(item => item.IdempotencyKey == $"service-request-expired:{request.PublicId:N}"));
    }
}
