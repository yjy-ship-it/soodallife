using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class SqlServerTriggerCompatibilityTests
{
    [Fact]
    public void Service_requests_disables_sql_output_clause_for_trigger_compatibility()
    {
        var options = new DbContextOptionsBuilder<SoodalLifeDbContext>()
            .UseSqlServer("Server=(local);Database=model-only;Trusted_Connection=True;TrustServerCertificate=True")
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        using var db = new SoodalLifeDbContext(options);
        var entityType = db.Model.FindEntityType(typeof(ServiceRequest));
        Assert.NotNull(entityType);

        var table = StoreObjectIdentifier.Table("service_requests", schema: null);
        Assert.False(entityType!.IsSqlOutputClauseUsed(table));
    }
}
