using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.Chat;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class CommonChatExpansionApiTests(AuthenticationWebApplicationFactory factory) : IClassFixture<AuthenticationWebApplicationFactory>
{
    [Fact]
    public async Task SubscriptionRoom_AllowsCurrentParties_PausedContract_AndRejectsUnselectedProvider()
    {
        var id = await SeedSubscription("PAUSED");
        using var customer = Client(); using var provider = Client(); using var other = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        await Login(other, factory.AreaMismatchProviderCredential);
        var room = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/subscriptions/{id}/room");
        Assert.NotNull(room); Assert.Equal("SUBSCRIPTION", room!.ResourceTypeCode); Assert.Equal(id, room.ResourceId);
        Guid visitId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var contract = await db.SubscriptionContracts.SingleAsync(x => x.PublicId == id);
            var visit = new SubscriptionVisitSchedule { SubscriptionContractId = contract.Id, ProviderProfileId = contract.ProviderProfileId,
                VisitNo = 1, ScheduledStartAt = DateTime.UtcNow.AddDays(1), StatusCode = "SCHEDULED", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.SubscriptionVisitSchedules.Add(visit); await db.SaveChangesAsync(); visitId = visit.PublicId;
        }
        Assert.Equal(room.Id, (await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/subscription-visits/{visitId}/room"))!.Id);
        Assert.Equal(HttpStatusCode.OK, (await provider.GetAsync($"/api/v1/chat/rooms/{room.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/chat/rooms/{room.Id}")).StatusCode);
        (await provider.PostAsJsonAsync($"/api/v1/chat/rooms/{room.Id}/messages/text", new { body = "교체 전 기록", idempotencyKey = Guid.NewGuid().ToString("N") })).EnsureSuccessStatusCode();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>(); var item = await db.SubscriptionContracts.SingleAsync(x => x.PublicId == id);
            item.ProviderProfileId = await ProviderId(db, factory.AreaMismatchProviderCredential.LoginId); var replacedAt = DateTime.UtcNow.AddMilliseconds(10); item.UpdatedAt = replacedAt;
            db.SubscriptionEvents.Add(new SubscriptionEvent { SubscriptionContractId = item.Id, EventTypeCode = "PROVIDER_REPLACED", OccurredAt = replacedAt, IdempotencyKey = Guid.NewGuid().ToString("N") });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NotFound, (await provider.GetAsync($"/api/v1/chat/rooms/{room.Id}")).StatusCode);
        var replacementRoom = await other.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/subscriptions/{id}/room");
        Assert.Equal(room.Id, replacementRoom!.Id);
        Assert.Empty((await other.GetFromJsonAsync<ChatMessagePage>($"/api/v1/chat/rooms/{room.Id}/messages"))!.Items);
    }

    [Fact]
    public async Task InteriorRooms_AreSeparatedByAuthorizedRole_AndInactiveParticipantIsDenied()
    {
        var ids = await SeedInterior();
        using var customer = Client(); using var provider = Client(); using var survey = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        await Login(survey, factory.AreaMismatchProviderCredential);
        var primary = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/interiors/{ids.Project}/room?role=PRIMARY_CONTRACTOR");
        var site = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/interiors/{ids.Project}/room?role=SITE_SURVEY");
        Assert.NotNull(primary); Assert.NotNull(site); Assert.NotEqual(primary!.Id, site!.Id);
        Assert.Equal(HttpStatusCode.OK, (await provider.GetAsync($"/api/v1/chat/rooms/{primary.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await survey.GetAsync($"/api/v1/chat/rooms/{primary.Id}")).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var participant = await db.InteriorProjectParticipants.SingleAsync(x => x.PublicId == ids.PrimaryParticipant);
        participant.StatusCode = "ENDED"; participant.EffectiveTo = DateTime.UtcNow; await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await provider.GetAsync($"/api/v1/chat/rooms/{primary.Id}")).StatusCode);
    }

    [Fact]
    public async Task AfterServiceReplacement_BlocksOldProvider_AndDoesNotExposeOldTranscript()
    {
        var id = await SeedAfterService();
        using var customer = Client(); using var provider = Client(); using var replacement = Client();
        await Login(customer, factory.Credentials[RoleCodes.Customer]); await Login(provider, factory.Credentials[RoleCodes.Provider]);
        await Login(replacement, factory.AreaMismatchProviderCredential);
        var room = await customer.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/after-services/{id}/room");
        (await provider.PostAsJsonAsync($"/api/v1/chat/rooms/{room!.Id}/messages/text", new { body = "이전 담당자 기록", idempotencyKey = Guid.NewGuid().ToString("N") })).EnsureSuccessStatusCode();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
            var replacementId = await ProviderId(db, factory.AreaMismatchProviderCredential.LoginId);
            var item = await db.AfterServiceCases.SingleAsync(x => x.PublicId == id);
            item.ProviderProfileId = replacementId; item.UpdatedAt = DateTime.UtcNow.AddMilliseconds(10); await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NotFound, (await provider.GetAsync($"/api/v1/chat/rooms/{room.Id}")).StatusCode);
        var replacementRoom = await replacement.GetFromJsonAsync<ChatRoomDetail>($"/api/v1/chat/after-services/{id}/room");
        Assert.Equal(room.Id, replacementRoom!.Id);
        var messages = await replacement.GetFromJsonAsync<ChatMessagePage>($"/api/v1/chat/rooms/{room.Id}/messages");
        Assert.Empty(messages!.Items);
    }

    [Fact]
    public async Task TerminatedSubscription_DoesNotInventNewRoomCreationPolicy()
    {
        var id = await SeedSubscription("TERMINATED"); using var customer = Client(); await Login(customer, factory.Credentials[RoleCodes.Customer]);
        Assert.Equal(HttpStatusCode.Conflict, (await customer.GetAsync($"/api/v1/chat/subscriptions/{id}/room")).StatusCode);
    }

    private async Task<Guid> SeedSubscription(string status)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var customer = await CustomerId(db); var provider = await ProviderId(db, factory.Credentials[RoleCodes.Provider].LoginId);
        var category = await db.ServiceCategories.SingleAsync(x => x.PublicId == factory.Catalog.ServiceId); var now = DateTime.UtcNow;
        var item = new SubscriptionContract { CustomerProfileId = customer, ProviderProfileId = provider, ServiceCategoryId = category.Id,
            StatusCode = status, StartedAt = now, ServiceScopeSnapshotJson = "{}", RecurrenceSnapshotJson = "{}", CompletionPolicySnapshotJson = "{}",
            PriceSnapshotJson = "{}", CreatedAt = now, UpdatedAt = now };
        db.SubscriptionContracts.Add(item); await db.SaveChangesAsync(); return item.PublicId;
    }

    private async Task<(Guid Project, Guid PrimaryParticipant)> SeedInterior()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var customer = await CustomerId(db); var primary = await ProviderId(db, factory.Credentials[RoleCodes.Provider].LoginId);
        var survey = await ProviderId(db, factory.AreaMismatchProviderCredential.LoginId);
        var category = await db.ServiceCategories.SingleAsync(x => x.PublicId == factory.Catalog.ServiceId); var now = DateTime.UtcNow;
        var project = new InteriorProject { CustomerProfileId = customer, ServiceCategoryId = category.Id, SelectedContractorProviderId = primary,
            SelectedSiteVisitProviderId = survey, StatusCode = "CONTRACT", CreatedAt = now, UpdatedAt = now };
        db.InteriorProjects.Add(project); await db.SaveChangesAsync();
        var primaryParticipant = new InteriorProjectParticipant { InteriorProjectId = project.Id, ProviderProfileId = primary, RoleCode = "PRIMARY_CONTRACTOR", StatusCode = "ACTIVE", IsPrimary = true, EffectiveFrom = now, CreatedAt = now, UpdatedAt = now };
        db.InteriorProjectParticipants.Add(primaryParticipant);
        db.InteriorProjectParticipants.Add(new InteriorProjectParticipant { InteriorProjectId = project.Id, ProviderProfileId = survey, RoleCode = "SITE_SURVEY", StatusCode = "ACTIVE", IsPrimary = true, EffectiveFrom = now, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync(); return (project.PublicId, primaryParticipant.PublicId);
    }

    private async Task<Guid> SeedAfterService()
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var now = DateTime.UtcNow; var item = new AfterServiceCase { CustomerProfileId = await CustomerId(db),
            ProviderProfileId = await ProviderId(db, factory.Credentials[RoleCodes.Provider].LoginId), Subject = "A/S 채팅 테스트", Description = "접수 내용",
            StatusCode = "IN_PROGRESS", ReceivedAt = now, CreatedAt = now, UpdatedAt = now, IdempotencyKey = Guid.NewGuid().ToString("N") };
        db.AfterServiceCases.Add(item); await db.SaveChangesAsync(); return item.PublicId;
    }

    private async Task<long> CustomerId(SoodalLifeDbContext db) => await (from user in db.Users join profile in db.CustomerProfiles on user.Id equals profile.UserId where user.LoginId == factory.Credentials[RoleCodes.Customer].LoginId select profile.Id).SingleAsync();
    private static async Task<long> ProviderId(SoodalLifeDbContext db, string loginId) => await (from user in db.Users join profile in db.ProviderProfiles on user.Id equals profile.UserId where user.LoginId == loginId select profile.Id).SingleAsync();
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    private static Task<HttpResponseMessage> Login(HttpClient client, TestCredential credential) => client.PostAsJsonAsync("/api/v1/auth/login", new { LoginOrEmail = credential.LoginId, credential.Password });
}
