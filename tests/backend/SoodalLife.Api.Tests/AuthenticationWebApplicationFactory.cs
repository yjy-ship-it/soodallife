using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Tests;

public sealed class AuthenticationWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"authentication-tests-{Guid.NewGuid():N}";

    public IReadOnlyDictionary<string, TestCredential> Credentials { get; } =
        RoleCodes.All.ToDictionary(
            role => role,
            role => new TestCredential(
                $"test-{role.ToLowerInvariant()}-{Guid.NewGuid():N}",
                Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
            StringComparer.Ordinal);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SoodalLife"] = "Server=(local);Database=authentication-tests;Trusted_Connection=True;",
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SoodalLifeDbContext>>();
            services.RemoveAll<SoodalLifeDbContext>();
            services.AddDbContext<SoodalLifeDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        SeedAuthenticationData(host.Services);
        return host;
    }

    private void SeedAuthenticationData(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SoodalLifeDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        foreach (var (roleCode, credential) in Credentials)
        {
            var role = new Role { Code = roleCode, Name = roleCode, IsActive = true };
            dbContext.Roles.Add(role);

            var user = new User
            {
                LoginId = credential.LoginId,
                NormalizedLoginId = credential.LoginId.ToUpperInvariant(),
                StatusCode = "ACTIVE",
            };
            user.PasswordHash = passwordHasher.HashPassword(user, credential.Password);
            dbContext.Users.Add(user);
            dbContext.SaveChanges();

            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            dbContext.SaveChanges();
        }
    }
}

public sealed record TestCredential(string LoginId, string Password);
