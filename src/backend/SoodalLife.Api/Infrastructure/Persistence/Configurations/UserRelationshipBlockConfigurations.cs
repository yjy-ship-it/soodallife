using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoodalLife.Api.Domain.Entities;

namespace SoodalLife.Api.Infrastructure.Persistence.Configurations;

internal sealed class UserRelationshipBlockConfiguration() : EntityConfiguration<UserRelationshipBlock>("user_relationship_blocks")
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserRelationshipBlock> b)
    {
        Mapping.PublicId(b);
        Mapping.Long(b, nameof(UserRelationshipBlock.CustomerProfileId), "customer_profile_id");
        Mapping.Long(b, nameof(UserRelationshipBlock.ProviderProfileId), "provider_profile_id");
        Mapping.String(b, nameof(UserRelationshipBlock.DirectionCode), "direction_code", 30, unicode: false);
        Mapping.String(b, nameof(UserRelationshipBlock.StatusCode), "status_code", 20, unicode: false, defaultValue: "ACTIVE");
        Mapping.String(b, nameof(UserRelationshipBlock.ReasonCode), "reason_code", 50, nullable: true, unicode: false);
        Mapping.String(b, nameof(UserRelationshipBlock.PrivateMemo), "private_memo", 1000, nullable: true);
        Mapping.DateTime(b, nameof(UserRelationshipBlock.ReleasedAt), "released_at", nullable: true);
        Mapping.NullableLong(b, nameof(UserRelationshipBlock.ReleasedByUserId), "released_by_user_id");
        Mapping.String(b, nameof(UserRelationshipBlock.IdempotencyKey), "idempotency_key", 150, unicode: false);
        Mapping.String(b, nameof(UserRelationshipBlock.ReleaseIdempotencyKey), "release_idempotency_key", 150, nullable: true, unicode: false);
        Mapping.FullAudit(b);
        Mapping.Fk<UserRelationshipBlock, CustomerProfile>(b, nameof(UserRelationshipBlock.CustomerProfileId));
        Mapping.Fk<UserRelationshipBlock, ProviderProfile>(b, nameof(UserRelationshipBlock.ProviderProfileId));
        Mapping.Fk<UserRelationshipBlock, User>(b, nameof(UserRelationshipBlock.ReleasedByUserId));
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => x.ReleaseIdempotencyKey).IsUnique().HasFilter("[release_idempotency_key] IS NOT NULL");
        b.HasIndex(x => new { x.CustomerProfileId, x.ProviderProfileId, x.DirectionCode })
            .IsUnique().HasFilter("[status_code] = 'ACTIVE'");
        b.HasIndex(x => new { x.CustomerProfileId, x.StatusCode, x.CreatedAt });
        b.ToTable("user_relationship_blocks", t =>
        {
            t.HasCheckConstraint("CK_user_relationship_blocks_direction", "[direction_code] IN ('CUSTOMER_TO_PROVIDER','PROVIDER_TO_CUSTOMER')");
            t.HasCheckConstraint("CK_user_relationship_blocks_status", "[status_code] IN ('ACTIVE','RELEASED')");
        });
    }
}
