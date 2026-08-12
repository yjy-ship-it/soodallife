using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Providers;

public sealed partial class ProviderRegistrationService(SoodalLifeDbContext db, IPasswordHasher<User> passwordHasher)
{
    public async Task<IReadOnlyList<ProviderLegalDocumentResponse>> ActiveDocumentsAsync(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var rows = await (from document in db.LegalDocuments.AsNoTracking()
                          join version in db.LegalDocumentVersions.AsNoTracking() on document.Id equals version.LegalDocumentId
                          where document.IsActive && (document.AudienceCode == "PROVIDER" || document.AudienceCode == "ALL") &&
                                version.IsActive && version.EffectiveFrom <= now && (version.EffectiveTo == null || version.EffectiveTo > now)
                          orderby document.DisplayOrder, version.VersionNo descending
                          select new ProviderLegalDocumentResponse(document.PublicId, version.PublicId, document.Code,
                              document.RequirementCode, version.Title, version.Content, version.VersionNo,
                              version.EffectiveFrom, version.EffectiveTo, document.IsPlaceholder || version.IsPlaceholder)).ToListAsync(token);
        return rows.GroupBy(x => x.Id).Select(x => x.First()).ToArray();
    }

    public async Task<bool> LoginIdAvailableAsync(string value, CancellationToken token)
    {
        var login = value.Trim();
        if (!LoginPattern().IsMatch(login)) throw Invalid("LOGIN_ID_INVALID", "아이디는 영문으로 시작하고 영문, 숫자, -, _를 사용한 4~256자로 입력해 주세요.");
        return !await db.Users.AsNoTracking().AnyAsync(x => x.NormalizedLoginId == login.ToUpperInvariant(), token);
    }

    public async Task<ProviderRegistrationResponse> RegisterAsync(RegisterProviderRequest input, HttpContext context, CancellationToken token)
    {
        ValidateProfile(input.BusinessName, input.RepresentativeName, input.ContactName);
        var login = input.LoginId.Trim();
        if (!LoginPattern().IsMatch(login)) throw Invalid("LOGIN_ID_INVALID", "아이디는 영문으로 시작하고 영문, 숫자, -, _를 사용한 4~256자로 입력해 주세요.");
        ValidatePassword(input.Password, input.PasswordConfirmation);
        var email = NormalizeEmail(input.Email);
        var phone = NormalizePhone(input.Phone);
        var businessNo = NormalizeBusinessNumber(input.BusinessRegistrationNumber);
        if (await db.Users.AnyAsync(x => x.NormalizedLoginId == login.ToUpperInvariant(), token)) throw Conflict("LOGIN_ID_DUPLICATE", "이미 사용 중인 아이디입니다.");
        if (email is not null && await db.Users.AnyAsync(x => x.NormalizedEmail == email.ToUpperInvariant(), token)) throw Conflict("EMAIL_DUPLICATE", "이미 사용 중인 이메일입니다.");
        if (businessNo is not null && await db.ProviderProfiles.AnyAsync(x => x.BusinessRegistrationNo == businessNo, token)) throw Conflict("BUSINESS_NUMBER_DUPLICATE", "이미 등록된 사업자등록번호입니다.");
        var legal = await ValidateConsentsAsync(input.Consents, token);
        var role = await ProviderRoleAsync(token);
        var now = DateTime.UtcNow;
        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        try
        {
            var user = new User
            {
                LoginId = login, NormalizedLoginId = login.ToUpperInvariant(), Email = email,
                NormalizedEmail = email?.ToUpperInvariant(), Phone = phone, EmailVerificationStatusCode = "NOT_INTEGRATED",
                PhoneVerificationStatusCode = "NOT_INTEGRATED", StatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, input.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync(token);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = now });
            var profile = CreateProfile(user.Id, input.BusinessName, input.RepresentativeName, input.ContactName,
                businessNo, input.BusinessAddress, input.BusinessTypeText, input.BusinessItemText, input.Introduction, now, user.Id);
            db.ProviderProfiles.Add(profile);
            AddConsents(user.Id, legal, input.Consents, context, now);
            await db.SaveChangesAsync(token);
            if (transaction is not null) await transaction.CommitAsync(token);
            return new(user.PublicId, profile.PublicId, user.LoginId, [RoleCodes.Provider], profile.ApprovalStatusCode, profile.ActivityStatusCode);
        }
        catch (DbUpdateException)
        {
            if (transaction is not null) await transaction.RollbackAsync(token);
            throw Conflict("PROVIDER_REGISTRATION_DUPLICATE", "이미 등록된 계정 또는 사업자정보입니다.");
        }
        finally { if (transaction is not null) await transaction.DisposeAsync(); }
    }

    public async Task<ProviderRegistrationResponse> AddRoleAsync(ClaimsPrincipal principal, AddProviderRoleRequest input, HttpContext context, CancellationToken token)
    {
        ValidateProfile(input.BusinessName, input.RepresentativeName, input.ContactName);
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId)) throw Invalid("USER_ID_INVALID", "로그인 정보를 확인할 수 없습니다.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.PublicId == publicId && x.StatusCode == "ACTIVE", token)
            ?? throw new ProviderConfigurationException("ACTIVE_USER_NOT_FOUND", "활성 계정을 찾을 수 없습니다.", StatusCodes.Status404NotFound);
        if (await db.ProviderProfiles.AnyAsync(x => x.UserId == user.Id, token)) throw Conflict("PROVIDER_ROLE_ALREADY_EXISTS", "이미 공급자 역할을 보유하고 있습니다.");
        var businessNo = NormalizeBusinessNumber(input.BusinessRegistrationNumber);
        if (businessNo is not null && await db.ProviderProfiles.AnyAsync(x => x.BusinessRegistrationNo == businessNo, token)) throw Conflict("BUSINESS_NUMBER_DUPLICATE", "이미 등록된 사업자등록번호입니다.");
        var legal = await ValidateConsentsAsync(input.Consents, token);
        var role = await ProviderRoleAsync(token);
        var now = DateTime.UtcNow;
        var userRole = await db.UserRoles.SingleOrDefaultAsync(x => x.UserId == user.Id && x.RoleId == role.Id, token);
        if (userRole is null) db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = now });
        else { userRole.RevokedAt = null; userRole.RevokedByUserId = null; userRole.GrantedAt = now; }
        var profile = CreateProfile(user.Id, input.BusinessName, input.RepresentativeName, input.ContactName,
            businessNo, input.BusinessAddress, input.BusinessTypeText, input.BusinessItemText, input.Introduction, now, user.Id);
        db.ProviderProfiles.Add(profile);
        AddConsents(user.Id, legal, input.Consents, context, now);
        await db.SaveChangesAsync(token);
        var roles = await (from item in db.UserRoles.AsNoTracking() join itemRole in db.Roles.AsNoTracking() on item.RoleId equals itemRole.Id
                           where item.UserId == user.Id && item.RevokedAt == null && itemRole.IsActive select itemRole.Code).ToListAsync(token);
        return new(user.PublicId, profile.PublicId, user.LoginId, roles.OrderBy(x => x).ToArray(), profile.ApprovalStatusCode, profile.ActivityStatusCode);
    }

    private async Task<List<(LegalDocument Document, LegalDocumentVersion Version)>> ValidateConsentsAsync(IReadOnlyList<ProviderConsentInput>? consents, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var active = await (from document in db.LegalDocuments
                            join version in db.LegalDocumentVersions on document.Id equals version.LegalDocumentId
                            where document.IsActive && (document.AudienceCode == "PROVIDER" || document.AudienceCode == "ALL") && version.IsActive &&
                                  version.EffectiveFrom <= now && (version.EffectiveTo == null || version.EffectiveTo > now)
                            orderby version.VersionNo descending select new { document, version }).ToListAsync(token);
        var legal = active.GroupBy(x => x.document.Id).Select(x => x.First()).Select(x => (x.document, x.version)).ToList();
        var map = (consents ?? []).GroupBy(x => x.LegalDocumentVersionId).ToDictionary(x => x.Key, x => x.Last().Agreed);
        if (legal.Any(x => x.document.RequirementCode == "REQUIRED" && !map.GetValueOrDefault(x.version.PublicId)))
            throw Invalid("REQUIRED_CONSENT_MISSING", "필수 약관에 동의해 주세요.");
        if (map.Keys.Any(id => legal.All(x => x.version.PublicId != id))) throw Invalid("LEGAL_VERSION_INVALID", "현재 사용할 수 없는 약관 버전입니다.");
        return legal;
    }

    private void AddConsents(long userId, IReadOnlyList<(LegalDocument Document, LegalDocumentVersion Version)> legal,
        IReadOnlyList<ProviderConsentInput>? inputs, HttpContext context, DateTime now)
    {
        var map = (inputs ?? []).GroupBy(x => x.LegalDocumentVersionId).ToDictionary(x => x.Key, x => x.Last().Agreed);
        foreach (var item in legal.Where(x => map.GetValueOrDefault(x.Version.PublicId)))
            db.UserConsents.Add(new UserConsent { UserId = userId, LegalDocumentVersionId = item.Version.Id,
                ConsentStatusCode = "CONSENTED", ConsentedAt = now, SourceCode = "PROVIDER_WEB_SIGNUP",
                IpAddress = context.Connection.RemoteIpAddress?.ToString(), UserAgent = Trim(context.Request.Headers.UserAgent.ToString(), 1000), CreatedAt = now });
    }

    private async Task<Role> ProviderRoleAsync(CancellationToken token) =>
        await db.Roles.SingleOrDefaultAsync(x => x.Code == RoleCodes.Provider && x.IsActive, token)
        ?? throw new ProviderConfigurationException("PROVIDER_ROLE_UNAVAILABLE", "공급자 역할을 사용할 수 없습니다.", StatusCodes.Status500InternalServerError);

    private static ProviderProfile CreateProfile(long userId, string businessName, string representativeName, string contactName,
        string? businessNo, string? address, string? typeText, string? itemText, string? introduction, DateTime now, long actor) => new()
    {
        UserId = userId, BusinessName = businessName.Trim(), RepresentativeName = representativeName.Trim(), ContactName = contactName.Trim(),
        BusinessRegistrationNo = businessNo, BusinessAddress = Trim(address, 500), BusinessTypeText = Trim(typeText, 100),
        BusinessItemText = Trim(itemText, 100), Introduction = Trim(introduction, 1000), ApprovalStatusCode = "PENDING",
        ActivityStatusCode = "INACTIVE", CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor,
    };

    private static void ValidateProfile(string businessName, string representativeName, string contactName)
    {
        if (string.IsNullOrWhiteSpace(businessName) || businessName.Trim().Length > 200) throw Invalid("BUSINESS_NAME_INVALID", "상호 또는 공급자 표시명을 200자 이하로 입력해 주세요.");
        if (string.IsNullOrWhiteSpace(representativeName) || representativeName.Trim().Length > 100) throw Invalid("REPRESENTATIVE_NAME_INVALID", "대표자명을 100자 이하로 입력해 주세요.");
        if (string.IsNullOrWhiteSpace(contactName) || contactName.Trim().Length > 100) throw Invalid("CONTACT_NAME_INVALID", "담당자명을 100자 이하로 입력해 주세요.");
    }
    private static void ValidatePassword(string value, string confirmation)
    {
        if (value != confirmation) throw Invalid("PASSWORD_CONFIRMATION_MISMATCH", "비밀번호 확인이 일치하지 않습니다.");
        if (value.Length < 12 || !value.Any(char.IsUpper) || !value.Any(char.IsLower) || !value.Any(char.IsDigit) || value.All(char.IsLetterOrDigit))
            throw Invalid("PASSWORD_POLICY_FAILED", "비밀번호는 12자 이상이며 영문 대·소문자, 숫자, 특수문자를 포함해야 합니다.");
    }
    private static string? NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = value.Trim().ToLowerInvariant();
        if (result.Length > 320 || !result.Contains('@') || result.StartsWith('@') || result.EndsWith('@')) throw Invalid("EMAIL_INVALID", "올바른 이메일을 입력해 주세요.");
        return result;
    }
    private static string? NormalizePhone(string? value) => string.IsNullOrWhiteSpace(value) ? null : new string(value.Where(char.IsDigit).ToArray()) is var phone && phone.Length is >= 9 and <= 15 ? phone : throw Invalid("PHONE_INVALID", "올바른 전화번호를 입력해 주세요.");
    private static string? NormalizeBusinessNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = new string(value.Where(char.IsDigit).ToArray());
        if (result.Length != 10) throw Invalid("BUSINESS_NUMBER_INVALID", "사업자등록번호는 숫자 10자리로 입력해 주세요.");
        return result;
    }
    private static string? Trim(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : throw Invalid("VALUE_TOO_LONG", $"{max}자 이하로 입력해 주세요.");
    private static ProviderConfigurationException Invalid(string code, string message) => new(code, message);
    private static ProviderConfigurationException Conflict(string code, string message) => new(code, message, StatusCodes.Status409Conflict);
    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_-]{3,255}$", RegexOptions.CultureInvariant)] private static partial Regex LoginPattern();
}
