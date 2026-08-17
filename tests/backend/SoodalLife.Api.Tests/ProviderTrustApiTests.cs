using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Trust;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class ProviderTrustApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task Provider_CanReadCurrentTrustGradesAndOwnHistory()
    {
        Guid eventId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var providerId = await (from user in db.Users join provider in db.ProviderProfiles on user.Id equals provider.UserId
                                    where user.LoginId == factory.Credentials[RoleCodes.Provider].LoginId select provider.Id).SingleAsync();
            var now = DateTime.UtcNow;
            var item = new TrustScoreEvent { ProviderProfileId = providerId, EventTypeCode = "MANUAL_ADJUSTMENT", SourceTypeCode = "TEST",
                IdempotencyKey = $"provider-trust-test-{Guid.NewGuid():N}", ScoreBefore = 70, ScoreDelta = 5, ScoreAfter = 75,
                GradeBefore = "TRUSTED", GradeAfter = "TRUSTED", ReasonText = "서비스 품질 확인 반영", OccurredAt = now, ProcessedAt = now, CreatedAt = now };
            db.TrustScoreEvents.Add(item); await db.SaveChangesAsync(); eventId = item.PublicId;
        }

        using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var response = await client.GetAsync(Path); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProviderTrustDashboardResponse>(); Assert.NotNull(result);
        Assert.Equal(5, result.Grades.Count); Assert.Contains(result.Events, x => x.Id == eventId && x.Reason == "서비스 품질 확인 반영");
        Assert.All(result.Grades, item => Assert.True(item.MaximumScore >= item.MinimumScore));
    }

    [Fact]
    public async Task DifferentProviders_ReceiveOnlyTheirOwnTrustData()
    {
        using var first = Client(); using var second = Client();
        await Login(first, factory.Credentials[RoleCodes.Provider]); await Login(second, factory.AreaMismatchProviderCredential);
        var firstResult = await first.GetFromJsonAsync<ProviderTrustDashboardResponse>(Path);
        var secondResult = await second.GetFromJsonAsync<ProviderTrustDashboardResponse>(Path);
        Assert.NotNull(firstResult); Assert.NotNull(secondResult); Assert.NotEqual(firstResult.ProviderId, secondResult.ProviderId);
    }

    [Theory]
    [InlineData(RoleCodes.Customer)]
    [InlineData(RoleCodes.Admin)]
    public async Task NonProvider_CannotReadProviderTrust(string role)
    {
        using var client = Client(); await Login(client, factory.Credentials[role]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Path)).StatusCode);
    }

    [Fact]
    public async Task ProviderTrust_DoesNotExposeInternalOrAdminOnlyData()
    {
        using var client = Client(); await Login(client, factory.Credentials[RoleCodes.Provider]);
        var json = await client.GetStringAsync(Path);
        Assert.DoesNotContain("providerProfileId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("idempotencyKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("processedByUserId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("policySnapshotJson", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sourceSnapshotJson", json, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
    private const string Path = "/api/v1/providers/me/trust";
}
