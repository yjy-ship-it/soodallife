using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class StoredFilePurposeConstraintTests
{
    [Fact]
    public void FilesPurposeConstraint_AllowsChatAttachments()
    {
        var options = new DbContextOptionsBuilder<SoodalLifeDbContext>()
            .UseInMemoryDatabase($"file-purpose-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        using var db = new SoodalLifeDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(StoredFile));
        var constraint = entity!.GetCheckConstraints()
            .Single(x => x.Name == "CK_files_purpose");

        Assert.Contains("'CHAT_ATTACHMENT'", constraint.Sql, StringComparison.Ordinal);
    }
}
