using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SoodalLife.Api.Infrastructure.Persistence;

public sealed class SoodalLifeDbContextFactory : IDesignTimeDbContextFactory<SoodalLifeDbContext>
{
    public SoodalLifeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SoodalLifeDbContext>()
            .UseSqlServer(connectionString: null)
            .Options;

        return new SoodalLifeDbContext(options);
    }
}
