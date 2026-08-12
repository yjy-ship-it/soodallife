using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Chat;

public static class ChatResourceTypes
{
    public const string Transaction = "TRANSACTION";
    public const string Subscription = "SUBSCRIPTION";
    public const string Interior = "INTERIOR";
    public const string AfterService = "AFTER_SERVICE";
}

public sealed record ChatResourceAuthorization(
    string ResourceTypeCode, Guid ResourceId, string RoomTypeCode, long CustomerUserId, long ProviderUserId,
    string ServiceName, string ResourceNumber, DateTime CustomerAccessStartedAt, DateTime ProviderAccessStartedAt,
    bool CanCreateRoom);

public interface IChatResourceAuthorizationResolver
{
    Task<ChatResourceAuthorization?> ResolveAsync(string resourceTypeCode, Guid resourceId, string roomTypeCode, CancellationToken token);
}

public sealed class ChatResourceAuthorizationResolver(SoodalLifeDbContext db) : IChatResourceAuthorizationResolver
{
    public async Task<ChatResourceAuthorization?> ResolveAsync(string resourceTypeCode, Guid resourceId, string roomTypeCode, CancellationToken token)
    {
        resourceTypeCode = resourceTypeCode.Trim().ToUpperInvariant();
        roomTypeCode = roomTypeCode.Trim().ToUpperInvariant();
        return resourceTypeCode switch
        {
            ChatResourceTypes.Transaction when roomTypeCode == "DIRECT" => await Transaction(resourceId, token),
            ChatResourceTypes.Subscription when roomTypeCode == "DIRECT" => await Subscription(resourceId, token),
            ChatResourceTypes.Interior when roomTypeCode is "PRIMARY_CONTRACTOR" or "SITE_SURVEY" => await Interior(resourceId, roomTypeCode, token),
            ChatResourceTypes.AfterService when roomTypeCode == "DIRECT" => await AfterService(resourceId, token),
            _ => null,
        };
    }

    private async Task<ChatResourceAuthorization?> Transaction(Guid id, CancellationToken token) =>
        await (from item in db.Transactions.AsNoTracking()
               join customer in db.CustomerProfiles.AsNoTracking() on item.CustomerProfileId equals customer.Id
               join provider in db.ProviderProfiles.AsNoTracking() on item.ProviderProfileId equals provider.Id
               join category in db.ServiceCategories.AsNoTracking() on item.CategoryId equals category.Id
               where item.PublicId == id
               select new ChatResourceAuthorization(ChatResourceTypes.Transaction, item.PublicId, "DIRECT", customer.UserId,
                   provider.UserId, category.Name, Number("TR", item.PublicId), item.CreatedAt, item.CreatedAt, true))
            .SingleOrDefaultAsync(token);

    private async Task<ChatResourceAuthorization?> Subscription(Guid id, CancellationToken token)
    {
        var row = await (from item in db.SubscriptionContracts.AsNoTracking()
                         join customer in db.CustomerProfiles.AsNoTracking() on item.CustomerProfileId equals customer.Id
                         join provider in db.ProviderProfiles.AsNoTracking() on item.ProviderProfileId equals provider.Id
                         join category in db.ServiceCategories.AsNoTracking() on item.ServiceCategoryId equals category.Id
                         where item.PublicId == id
                         select new { item, CustomerUserId = customer.UserId, ProviderUserId = provider.UserId, ServiceName = category.Name })
            .SingleOrDefaultAsync(token);
        if (row is null) return null;
        var replacementAt = await db.SubscriptionEvents.AsNoTracking()
            .Where(x => x.SubscriptionContractId == row.item.Id && x.EventTypeCode == "PROVIDER_REPLACED")
            .OrderByDescending(x => x.OccurredAt).Select(x => (DateTime?)x.OccurredAt).FirstOrDefaultAsync(token);
        return new(ChatResourceTypes.Subscription, row.item.PublicId, "DIRECT", row.CustomerUserId, row.ProviderUserId,
            row.ServiceName, Number("SC", row.item.PublicId), row.item.CreatedAt, replacementAt ?? row.item.CreatedAt,
            row.item.StatusCode != "TERMINATED" && row.item.StatusCode != "COMPLETED");
    }

    private async Task<ChatResourceAuthorization?> Interior(Guid id, string roomType, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var role = roomType == "SITE_SURVEY" ? "SITE_SURVEY" : "PRIMARY_CONTRACTOR";
        return await (from project in db.InteriorProjects.AsNoTracking()
                      join customer in db.CustomerProfiles.AsNoTracking() on project.CustomerProfileId equals customer.Id
                      join category in db.ServiceCategories.AsNoTracking() on project.ServiceCategoryId equals category.Id
                      join participant in db.InteriorProjectParticipants.AsNoTracking() on project.Id equals participant.InteriorProjectId
                      join provider in db.ProviderProfiles.AsNoTracking() on participant.ProviderProfileId equals provider.Id
                      where project.PublicId == id && participant.RoleCode == role && participant.StatusCode == "ACTIVE" &&
                            participant.EffectiveFrom <= now && (participant.EffectiveTo == null || participant.EffectiveTo > now) &&
                            (roomType == "SITE_SURVEY" ? project.SelectedSiteVisitProviderId == participant.ProviderProfileId :
                                project.SelectedContractorProviderId == participant.ProviderProfileId)
                      orderby participant.IsPrimary descending, participant.EffectiveFrom descending
                      select new ChatResourceAuthorization(ChatResourceTypes.Interior, project.PublicId, roomType, customer.UserId,
                          provider.UserId, category.Name, Number(roomType == "SITE_SURVEY" ? "IS" : "IP", project.PublicId),
                          project.CreatedAt, participant.EffectiveFrom, true))
            .FirstOrDefaultAsync(token);
    }

    private async Task<ChatResourceAuthorization?> AfterService(Guid id, CancellationToken token)
    {
        var row = await (from item in db.AfterServiceCases.AsNoTracking()
                         join customer in db.CustomerProfiles.AsNoTracking() on item.CustomerProfileId equals customer.Id
                         join provider in db.ProviderProfiles.AsNoTracking() on item.ProviderProfileId equals provider.Id
                         where item.PublicId == id
                         select new { item, CustomerUserId = customer.UserId, ProviderUserId = provider.UserId, provider.BusinessName })
            .SingleOrDefaultAsync(token);
        if (row is null) return null;
        var providerUserId = row.ProviderUserId;
        var providerStart = row.item.UpdatedAt > row.item.ReceivedAt ? row.item.UpdatedAt : row.item.ReceivedAt;
        if (row.item.InteriorProjectId.HasValue)
        {
            var now = DateTime.UtcNow;
            var assigned = await (from participant in db.InteriorProjectParticipants.AsNoTracking()
                                  join provider in db.ProviderProfiles.AsNoTracking() on participant.ProviderProfileId equals provider.Id
                                  where participant.InteriorProjectId == row.item.InteriorProjectId && participant.RoleCode == "AFTER_SERVICE" &&
                                        participant.StatusCode == "ACTIVE" && participant.EffectiveFrom <= now &&
                                        (participant.EffectiveTo == null || participant.EffectiveTo > now)
                                  orderby participant.IsPrimary descending, participant.EffectiveFrom descending
                                  select new { provider.UserId, participant.EffectiveFrom }).FirstOrDefaultAsync(token);
            if (assigned is not null) { providerUserId = assigned.UserId; providerStart = assigned.EffectiveFrom; }
        }
        return new(ChatResourceTypes.AfterService, row.item.PublicId, "DIRECT", row.CustomerUserId, providerUserId,
            row.item.Subject, Number("AS", row.item.PublicId), row.item.ReceivedAt, providerStart, true);
    }

    private static string Number(string prefix, Guid id) => $"{prefix}-{id.ToString("N")[..10].ToUpperInvariant()}";
}
