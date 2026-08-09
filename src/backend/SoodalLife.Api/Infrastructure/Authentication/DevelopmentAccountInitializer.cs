using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Infrastructure.Authentication;

public sealed class DevelopmentAccountInitializer(
    SoodalLifeDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<DevelopmentAccountInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DevelopmentAccounts:Enabled"))
        {
            return;
        }

        var accountDefinitions = new[]
        {
            ReadAccount("Customer", RoleCodes.Customer),
            ReadAccount("Provider", RoleCodes.Provider),
            ReadAccount("Admin", RoleCodes.Admin),
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var definition in accountDefinitions)
        {
            await EnsureAccountAsync(definition, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Development authentication accounts are ready for {AccountCount} roles.", accountDefinitions.Length);
    }

    private DevelopmentAccountDefinition ReadAccount(string sectionName, string roleCode)
    {
        var section = configuration.GetSection($"DevelopmentAccounts:{sectionName}");
        var loginId = RequireValue(section, "LoginId");
        var password = RequireValue(section, "Password");

        if (password.Length < 12)
        {
            throw new InvalidOperationException($"DevelopmentAccounts:{sectionName}:Password must contain at least 12 characters.");
        }

        return new DevelopmentAccountDefinition(
            roleCode,
            loginId,
            section["Email"],
            password,
            section["DisplayName"] ?? $"{sectionName} 개발 계정");
    }

    private async Task EnsureAccountAsync(DevelopmentAccountDefinition definition, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(candidate => candidate.Code == definition.RoleCode, cancellationToken);
        if (role is null)
        {
            role = new Role
            {
                Code = definition.RoleCode,
                Name = definition.RoleCode switch
                {
                    RoleCodes.Customer => "고객",
                    RoleCodes.Provider => "공급자",
                    RoleCodes.Admin => "관리자",
                    _ => definition.RoleCode,
                },
            };
            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var normalizedLoginId = definition.LoginId.Trim().ToUpperInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.NormalizedLoginId == normalizedLoginId,
            cancellationToken);

        if (user is null)
        {
            user = new User
            {
                LoginId = definition.LoginId.Trim(),
                NormalizedLoginId = normalizedLoginId,
                Email = string.IsNullOrWhiteSpace(definition.Email) ? null : definition.Email.Trim(),
                StatusCode = "ACTIVE",
            };
            user.PasswordHash = passwordHasher.HashPassword(user, definition.Password);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var hasRole = await dbContext.UserRoles.AnyAsync(
            candidate => candidate.UserId == user.Id && candidate.RoleId == role.Id && candidate.RevokedAt == null,
            cancellationToken);
        if (!hasRole)
        {
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        }

        if (definition.RoleCode == RoleCodes.Customer &&
            !await dbContext.CustomerProfiles.AnyAsync(candidate => candidate.UserId == user.Id, cancellationToken))
        {
            dbContext.CustomerProfiles.Add(new CustomerProfile
            {
                UserId = user.Id,
                DisplayName = definition.DisplayName,
            });
        }

        if (definition.RoleCode == RoleCodes.Provider &&
            !await dbContext.ProviderProfiles.AnyAsync(candidate => candidate.UserId == user.Id, cancellationToken))
        {
            dbContext.ProviderProfiles.Add(new ProviderProfile
            {
                UserId = user.Id,
                BusinessName = definition.DisplayName,
                ApprovalStatusCode = configuration.GetValue("DevelopmentAccounts:Provider:EnableMatching", true) ? "APPROVED" : "PENDING",
                ActivityStatusCode = configuration.GetValue("DevelopmentAccounts:Provider:EnableMatching", true) ? "ACTIVE" : "INACTIVE",
                ApprovalDecidedAt = configuration.GetValue("DevelopmentAccounts:Provider:EnableMatching", true) ? DateTime.UtcNow : null,
            });
        }
        else if (definition.RoleCode == RoleCodes.Provider)
        {
            var provider = await dbContext.ProviderProfiles.SingleAsync(candidate => candidate.UserId == user.Id, cancellationToken);
            var enableMatching = configuration.GetValue("DevelopmentAccounts:Provider:EnableMatching", true);
            provider.ApprovalStatusCode = enableMatching ? "APPROVED" : "PENDING";
            provider.ActivityStatusCode = enableMatching ? "ACTIVE" : "INACTIVE";
            provider.ApprovalDecidedAt = enableMatching ? provider.ApprovalDecidedAt ?? DateTime.UtcNow : null;
            provider.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string RequireValue(IConfigurationSection section, string key) =>
        !string.IsNullOrWhiteSpace(section[key])
            ? section[key]!
            : throw new InvalidOperationException($"Required development secret '{section.Path}:{key}' is not configured.");

    private sealed record DevelopmentAccountDefinition(
        string RoleCode,
        string LoginId,
        string? Email,
        string Password,
        string DisplayName);
}
