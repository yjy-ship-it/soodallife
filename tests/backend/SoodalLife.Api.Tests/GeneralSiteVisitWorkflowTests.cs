using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SoodalLife.Api.Controllers;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class GeneralSiteVisitWorkflowTests
{
    [Fact]
    public void Model_has_dedicated_site_visit_tables_and_unique_dispatch()
    {
        var options = new DbContextOptionsBuilder<SoodalLifeDbContext>()
            .UseSqlServer("Server=(local);Database=model-only;Trusted_Connection=True;TrustServerCertificate=True")
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        using var db=new SoodalLifeDbContext(options);
        var proposal=db.Model.FindEntityType(typeof(SiteVisitProposal));
        var events=db.Model.FindEntityType(typeof(SiteVisitEvent));
        Assert.Equal("site_visit_proposals",proposal?.GetTableName());
        Assert.Equal("site_visit_events",events?.GetTableName());
        Assert.Contains(proposal!.GetIndexes(),index=>index.IsUnique&&index.Properties.Single().Name==nameof(SiteVisitProposal.RequestDispatchId));
    }

    [Fact]
    public void Customer_and_provider_actions_are_role_restricted()
    {
        var methods=typeof(SiteVisitActionsController).GetMethods();
        var accept=methods.Single(method=>method.Name=="Accept");
        var progress=methods.Single(method=>method.Name=="Progress");
        Assert.Equal("CUSTOMER",accept.GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>().Single().Roles);
        Assert.Equal("PROVIDER",progress.GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>().Single().Roles);
    }
}
