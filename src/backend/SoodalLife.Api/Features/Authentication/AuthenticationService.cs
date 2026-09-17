using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;
using SoodalLife.Api.Features.Admin;

namespace SoodalLife.Api.Features.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticatedUserResponse?> AuthenticateAsync(string loginOrEmail, string password, string? mfaCode, CancellationToken cancellationToken);
    Task<AuthenticatedUserResponse?> CurrentAsync(Guid publicId, CancellationToken cancellationToken);
}

internal sealed class AuthenticationService(
    SoodalLifeDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IPersonalDataSearchHasher searchHasher,
    IAdminMfaVerifier adminMfaVerifier) : IAuthenticationService
{
    public async Task<AuthenticatedUserResponse?> AuthenticateAsync(
        string loginOrEmail,
        string password,
        string? mfaCode,
        CancellationToken cancellationToken)
    {
        var identifier = loginOrEmail.Trim();
        var normalizedIdentifier = identifier.ToUpperInvariant();

        User? user;
        if (identifier.Contains('@') && searchHasher.IsConfigured)
        {
            var hash = searchHasher.Email(identifier);
            user = await dbContext.Users.SingleOrDefaultAsync(candidate =>
                candidate.EmailSearchHash != null && candidate.EmailSearchHash.SequenceEqual(hash) ||
                candidate.EmailSearchHash == null && (candidate.NormalizedEmail == normalizedIdentifier || candidate.NormalizedEmail == null && candidate.Email != null && candidate.Email.ToUpper() == normalizedIdentifier), cancellationToken);
        }
        else user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.NormalizedLoginId == normalizedIdentifier, cancellationToken);

        if (user is null || user.StatusCode != "ACTIVE")
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var roles = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == user.Id && userRole.RevokedAt == null && role.IsActive
                orderby role.Code
                select role.Code)
            .ToListAsync(cancellationToken);

        if (roles.Count == 0)
        {
            return null;
        }

        if (roles.Contains(RoleCodes.Admin, StringComparer.Ordinal) && !await adminMfaVerifier.VerifyForLoginAsync(user.Id, mfaCode, cancellationToken))
        {
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthenticatedUserResponse(user.PublicId, user.LoginId, roles);
    }

    public async Task<AuthenticatedUserResponse?> CurrentAsync(Guid publicId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.PublicId == publicId && x.StatusCode == "ACTIVE", cancellationToken);
        if (user is null) return null;
        var roles = await (from userRole in dbContext.UserRoles.AsNoTracking()
                           join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                           where userRole.UserId == user.Id && userRole.RevokedAt == null && role.IsActive
                           orderby role.Code select role.Code).ToListAsync(cancellationToken);
        return roles.Count == 0 ? null : new AuthenticatedUserResponse(user.PublicId, user.LoginId, roles);
    }
}
