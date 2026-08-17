using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Features.CustomerAccounts;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Features.Providers;

public sealed partial class ProviderRegistrationService(SoodalLifeDbContext db, IPasswordHasher<User> passwordHasher,
    IIdentityVerificationAdapter identityVerification, IPersonalDataSearchHasher searchHasher)
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

    public async Task<BusinessRegistrationAvailabilityResponse> BusinessRegistrationNumberAvailabilityAsync(string value, CancellationToken token)
    {
        var normalizedValue = new string(value.Where(char.IsDigit).ToArray());
        var valid = IsValidBusinessRegistrationNumber(normalizedValue);
        var available = valid && !await db.ProviderProfiles.AsNoTracking()
            .AnyAsync(x => x.BusinessRegistrationNo == normalizedValue, token);
        return new(normalizedValue, valid, available);
    }

    public async Task<ProviderRegistrationResponse> RegisterAsync(RegisterProviderRequest input, HttpContext context, CancellationToken token)
    {
        ValidateProfile(input.BusinessName, input.RepresentativeName, input.ContactName);
        var login = input.LoginId.Trim();
        if (!LoginPattern().IsMatch(login)) throw Invalid("LOGIN_ID_INVALID", "아이디는 영문으로 시작하고 영문, 숫자, -, _를 사용한 4~256자로 입력해 주세요.");
        ValidatePassword(input.Password, input.PasswordConfirmation);
        var email = NormalizeEmail(input.Email);
        var phone = NormalizePhone(input.Phone);
        if (phone is null) throw Invalid("PHONE_REQUIRED", "휴대전화 번호를 입력해 주세요.");
        var verificationStatus = await identityVerification.GetStatusAsync(token);
        var verification = await identityVerification.VerifyPhoneAsync(phone, input.PhoneVerificationToken, token);
        var externalVerificationUnavailable = verificationStatus.StatusCode == "NOT_INTEGRATED";
        if (!externalVerificationUnavailable && (!verification.IsVerified || !string.Equals(NormalizePhone(verification.VerifiedPhone), phone, StringComparison.Ordinal)))
            throw Invalid("PHONE_IDENTITY_VERIFICATION_REQUIRED", "휴대전화 본인인증을 완료해 주세요.");
        if (externalVerificationUnavailable && input.PhoneVerificationToken != "NOT_INTEGRATED")
            throw Invalid("PHONE_CONFIRMATION_REQUIRED", "휴대전화 번호 확인 버튼을 눌러 주세요.");
        var businessNo = NormalizeBusinessNumber(input.BusinessRegistrationNumber);
        if (await db.Users.AnyAsync(x => x.NormalizedLoginId == login.ToUpperInvariant(), token)) throw Conflict("LOGIN_ID_DUPLICATE", "이미 사용 중인 아이디입니다.");
        var phoneExists = searchHasher.IsConfigured
            ? await db.Users.AnyAsync(x => x.PhoneSearchHash != null && x.PhoneSearchHash.SequenceEqual(searchHasher.Phone(phone)) || x.PhoneSearchHash == null && x.Phone == phone, token)
            : await db.Users.AnyAsync(x => x.Phone == phone, token);
        if (phoneExists) throw Conflict("PHONE_DUPLICATE", "이미 등록된 휴대전화 번호입니다.");
        if (email is not null && await db.Users.AnyAsync(x => x.NormalizedEmail == email.ToUpperInvariant(), token)) throw Conflict("EMAIL_DUPLICATE", "이미 사용 중인 이메일입니다.");
        if (businessNo is not null && await db.ProviderProfiles.AnyAsync(x => x.BusinessRegistrationNo == businessNo, token)) throw Conflict("BUSINESS_NUMBER_DUPLICATE", "이미 등록된 사업자등록번호입니다.");
        var legal = await ValidateConsentsAsync(input.Consents, token);
        var roles = await RegistrationRolesAsync(token);
        var now = DateTime.UtcNow;
        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        try
        {
            var user = new User
            {
                LoginId = login, NormalizedLoginId = login.ToUpperInvariant(), Email = email,
                NormalizedEmail = email?.ToUpperInvariant(), Phone = phone, EmailVerificationStatusCode = "NOT_INTEGRATED",
                PhoneVerificationStatusCode = externalVerificationUnavailable ? "NOT_INTEGRATED" : "VERIFIED", StatusCode = "ACTIVE", CreatedAt = now, UpdatedAt = now,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, input.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync(token);
            db.UserRoles.AddRange(roles.Select(role => new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = now }));
            db.CustomerProfiles.Add(new CustomerProfile { UserId = user.Id, DisplayName = input.RepresentativeName.Trim(), CreatedAt = now, CreatedByUserId = user.Id, UpdatedAt = now, UpdatedByUserId = user.Id });
            var profile = CreateProfile(user.Id, input.BusinessName, input.RepresentativeName, input.ContactName,
                businessNo, input.BusinessAddress, input.BusinessTypeText, input.BusinessItemText, input.Introduction, now, user.Id);
            db.ProviderProfiles.Add(profile);
            AddConsents(user.Id, legal, input.Consents, context, now);
            await db.SaveChangesAsync(token);
            db.ProviderWallets.Add(CreateInitialWallet(profile.Id, user.Id, now));
            await db.SaveChangesAsync(token);
            if (transaction is not null) await transaction.CommitAsync(token);
            return new(user.PublicId, profile.PublicId, user.LoginId, [RoleCodes.Customer, RoleCodes.Provider], profile.ApprovalStatusCode, profile.ActivityStatusCode);
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
        var verificationStatus = await identityVerification.GetStatusAsync(token);
        if (verificationStatus.StatusCode != "NOT_INTEGRATED" && user.PhoneVerificationStatusCode != "VERIFIED")
            throw Invalid("PHONE_IDENTITY_VERIFICATION_REQUIRED", "공급자 등록 전에 휴대전화 본인인증을 완료해 주세요.");
        if (await db.ProviderProfiles.AnyAsync(x => x.UserId == user.Id, token)) throw Conflict("PROVIDER_ROLE_ALREADY_EXISTS", "이미 공급자 역할을 보유하고 있습니다.");
        var businessNo = NormalizeBusinessNumber(input.BusinessRegistrationNumber);
        if (businessNo is not null && await db.ProviderProfiles.AnyAsync(x => x.BusinessRegistrationNo == businessNo, token)) throw Conflict("BUSINESS_NUMBER_DUPLICATE", "이미 등록된 사업자등록번호입니다.");
        var legal = await ValidateConsentsAsync(input.Consents, token);
        var role = await ProviderRoleAsync(token);
        var now = DateTime.UtcNow;
        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        try
        {
            var userRole = await db.UserRoles
                .Where(x => x.UserId == user.Id && x.RoleId == role.Id)
                .OrderBy(x => x.RevokedAt == null ? 0 : 1)
                .ThenByDescending(x => x.GrantedAt)
                .FirstOrDefaultAsync(token);
            if (userRole is null) db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = now });
            else { userRole.RevokedAt = null; userRole.RevokedByUserId = null; userRole.GrantedAt = now; }
            var profile = CreateProfile(user.Id, input.BusinessName, input.RepresentativeName, input.ContactName,
                businessNo, input.BusinessAddress, input.BusinessTypeText, input.BusinessItemText, input.Introduction, now, user.Id);
            db.ProviderProfiles.Add(profile);
            AddConsents(user.Id, legal, input.Consents, context, now);
            await db.SaveChangesAsync(token);
            db.ProviderWallets.Add(CreateInitialWallet(profile.Id, user.Id, now));
            await db.SaveChangesAsync(token);
            if (transaction is not null) await transaction.CommitAsync(token);
            var roles = await (from item in db.UserRoles.AsNoTracking() join itemRole in db.Roles.AsNoTracking() on item.RoleId equals itemRole.Id
                               where item.UserId == user.Id && item.RevokedAt == null && itemRole.IsActive select itemRole.Code).ToListAsync(token);
            return new(user.PublicId, profile.PublicId, user.LoginId, roles.OrderBy(x => x).ToArray(), profile.ApprovalStatusCode, profile.ActivityStatusCode);
        }
        catch (DbUpdateException)
        {
            if (transaction is not null) await transaction.RollbackAsync(token);
            throw Conflict("PROVIDER_REGISTRATION_DUPLICATE", "이미 등록된 공급자 역할 또는 Wallet입니다.");
        }
        finally { if (transaction is not null) await transaction.DisposeAsync(); }
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

    private async Task<Role[]> RegistrationRolesAsync(CancellationToken token)
    {
        var roles = await db.Roles.Where(x => x.IsActive && (x.Code == RoleCodes.Customer || x.Code == RoleCodes.Provider)).ToArrayAsync(token);
        if (roles.Length != 2) throw new ProviderConfigurationException("REGISTRATION_ROLE_UNAVAILABLE", "고객·공급자 역할을 사용할 수 없습니다.", StatusCodes.Status500InternalServerError);
        return roles;
    }

    private static ProviderProfile CreateProfile(long userId, string businessName, string representativeName, string contactName,
        string? businessNo, string? address, string? typeText, string? itemText, string? introduction, DateTime now, long actor) => new()
    {
        UserId = userId, BusinessName = businessName.Trim(), RepresentativeName = representativeName.Trim(), ContactName = contactName.Trim(),
        BusinessRegistrationNo = businessNo, BusinessAddress = Trim(address, 500), BusinessTypeText = Trim(typeText, 100),
        BusinessItemText = Trim(itemText, 100), Introduction = Trim(introduction, 1000), ApprovalStatusCode = "PENDING",
        ActivityStatusCode = "INACTIVE", CreatedAt = now, CreatedByUserId = actor, UpdatedAt = now, UpdatedByUserId = actor,
    };

    private static ProviderWallet CreateInitialWallet(long providerProfileId, long actorUserId, DateTime now) => new()
    {
        ProviderProfileId = providerProfileId,
        CurrencyCode = "KRW",
        AvailableBalance = 0,
        ReservedBalance = 0,
        StatusCode = "ACTIVE",
        CreatedAt = now,
        CreatedByUserId = actorUserId,
        UpdatedAt = now,
        UpdatedByUserId = actorUserId,
    };

    private static void ValidateProfile(string businessName, string representativeName, string contactName)
    {
        if (string.IsNullOrWhiteSpace(businessName) || businessName.Trim().Length > 200) throw Invalid("BUSINESS_NAME_INVALID", "상호 또는 공급자 표시명을 200자 이하로 입력해 주세요.");
        if (string.IsNullOrWhiteSpace(representativeName) || representativeName.Trim().Length > 100) throw Invalid("REPRESENTATIVE_NAME_INVALID", "대표자명을 100자 이하로 입력해 주세요.");
        if (string.IsNullOrWhiteSpace(contactName) || contactName.Trim().Length > 100) throw Invalid("CONTACT_NAME_INVALID", "담당자명을 100자 이하로 입력해 주세요.");
    }
    private static void ValidatePassword(string value, string confirmation)
    {
        if (value != confirmation) throw Invalid("PASSWORD_CONFIRMATION_MISMATCH", "비밀번호를 다시 확인하세요.");
        if (!AccountPasswordPolicy.IsSatisfied(value))
            throw Invalid("PASSWORD_POLICY_FAILED", "비밀번호가 규칙에 맞지 않습니다.");
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
        if (!IsValidBusinessRegistrationNumber(result)) throw Invalid("BUSINESS_NUMBER_INVALID", "유효한 사업자등록번호를 입력해 주세요.");
        return result;
    }
    private static bool IsValidBusinessRegistrationNumber(string value)
    {
        if (value.Length != 10 || value.Any(x => !char.IsDigit(x))) return false;
        var digits = value.Select(x => x - '0').ToArray();
        int[] weights = [1, 3, 7, 1, 3, 7, 1, 3, 5];
        var sum = weights.Select((weight, index) => weight * digits[index]).Sum();
        sum += digits[8] * 5 / 10;
        return (10 - sum % 10) % 10 == digits[9];
    }
    private static string? Trim(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : throw Invalid("VALUE_TOO_LONG", $"{max}자 이하로 입력해 주세요.");
    private static ProviderConfigurationException Invalid(string code, string message) => new(code, message);
    private static ProviderConfigurationException Conflict(string code, string message) => new(code, message, StatusCodes.Status409Conflict);
    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_-]{3,255}$", RegexOptions.CultureInvariant)] private static partial Regex LoginPattern();
}
