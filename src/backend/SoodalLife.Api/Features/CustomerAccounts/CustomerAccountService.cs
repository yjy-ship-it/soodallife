using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SoodalLife.Api.Features.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Features.CustomerAccounts;

public sealed partial class CustomerAccountService(
    SoodalLifeDbContext db,
    IPasswordHasher<User> passwordHasher,
    IPasswordResetDeliveryAdapter resetDelivery,
    IIdentityVerificationAdapter identityVerification,
    IPersonalDataSearchHasher searchHasher,
    IPersonalDataReader personalDataReader)
{
    public async Task<AvailabilityResponse> LoginIdAvailable(string value, CancellationToken token)
    {
        var normalized = NormalizeLogin(value);
        if (!LoginIdPattern().IsMatch(value.Trim())) throw Bad("LOGIN_ID_INVALID", "아이디는 영문자로 시작하고 영문, 숫자, -, _를 사용한 4~256자로 입력해 주세요.");
        return new(!await db.Users.AsNoTracking().AnyAsync(x => x.NormalizedLoginId == normalized, token), normalized);
    }

    public async Task<AvailabilityResponse> EmailAvailable(string value, CancellationToken token)
    {
        var normalized = NormalizeEmail(value);
        EnsureEmail(normalized);
        var exists = searchHasher.IsConfigured
            ? await db.Users.AsNoTracking().AnyAsync(x => x.EmailSearchHash != null && x.EmailSearchHash.SequenceEqual(searchHasher.Email(value)) || x.EmailSearchHash == null && (x.NormalizedEmail == normalized || x.NormalizedEmail == null && x.Email != null && x.Email.ToUpper() == normalized), token)
            : await db.Users.AsNoTracking().AnyAsync(x => x.NormalizedEmail == normalized || x.NormalizedEmail == null && x.Email != null && x.Email.ToUpper() == normalized, token);
        return new(!exists, normalized.ToLowerInvariant());
    }

    public async Task<AvailabilityResponse> PhoneAvailable(string value, CancellationToken token)
    {
        var phone = NormalizePhone(value);
        var exists = searchHasher.IsConfigured
            ? await db.Users.AsNoTracking().AnyAsync(x => x.PhoneSearchHash != null && x.PhoneSearchHash.SequenceEqual(searchHasher.Phone(phone)) || x.PhoneSearchHash == null && x.Phone == phone, token)
            : await db.Users.AsNoTracking().AnyAsync(x => x.Phone == phone, token);
        return new(!exists, FormatPhone(phone) ?? phone);
    }

    public async Task<List<LegalDocumentResponse>> ActiveDocuments(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var rows = await (from document in db.LegalDocuments.AsNoTracking()
                          join version in db.LegalDocumentVersions.AsNoTracking() on document.Id equals version.LegalDocumentId
                          where document.IsActive && (document.AudienceCode == "CUSTOMER" || document.AudienceCode == "ALL") &&
                                version.IsActive && version.EffectiveFrom <= now && (version.EffectiveTo == null || version.EffectiveTo > now)
                          orderby document.DisplayOrder, version.VersionNo descending
                          select new LegalDocumentResponse(document.PublicId, version.PublicId, document.Code, document.RequirementCode,
                              version.Title, version.Content, version.VersionNo, version.EffectiveFrom, version.EffectiveTo,
                              document.IsPlaceholder || version.IsPlaceholder))
            .ToListAsync(token);

        return rows.GroupBy(x => x.Id).Select(x => x.First()).ToList();
    }

    public async Task<CustomerRegistrationResponse> Register(RegisterCustomerRequest input, HttpContext context, CancellationToken token)
    {
        var login = input.LoginId.Trim();
        var normalizedLogin = NormalizeLogin(login);
        if (!LoginIdPattern().IsMatch(login)) throw Bad("LOGIN_ID_INVALID", "아이디는 영문자로 시작하고 영문, 숫자, -, _를 사용한 4~256자로 입력해 주세요.");
        var email = string.IsNullOrWhiteSpace(input.Email) ? null : NormalizeEmail(input.Email).ToLowerInvariant();
        if (email is not null) EnsureEmail(email);
        var phone = NormalizePhone(input.Phone);
        EnsurePassword(input.Password, input.PasswordConfirmation);
        var verificationStatus = await identityVerification.GetStatusAsync(token);
        var verification = await identityVerification.VerifyPhoneAsync(phone, input.PhoneVerificationToken, token);
        var externalVerificationUnavailable = verificationStatus.StatusCode == "NOT_INTEGRATED";
        if (!externalVerificationUnavailable && (!verification.IsVerified || !string.Equals(NormalizePhone(verification.VerifiedPhone ?? string.Empty), phone, StringComparison.Ordinal)))
            throw Bad("PHONE_IDENTITY_VERIFICATION_REQUIRED", "휴대전화 본인인증을 완료해 주세요.");
        if (externalVerificationUnavailable && input.PhoneVerificationToken != "NOT_INTEGRATED")
            throw Bad("PHONE_CONFIRMATION_REQUIRED", "휴대전화 번호 확인 버튼을 눌러 주세요.");

        if (await db.Users.AnyAsync(x => x.NormalizedLoginId == normalizedLogin, token)) throw Conflict("LOGIN_ID_DUPLICATE", "이미 사용 중인 아이디입니다.");
        if (!(await PhoneAvailable(phone, token)).Available) throw Conflict("PHONE_DUPLICATE", "이미 등록된 휴대전화 번호입니다.");
        if (email is not null && !(await EmailAvailable(email, token)).Available) throw Conflict("EMAIL_DUPLICATE", "이미 사용 중인 이메일입니다.");

        var now = DateTime.UtcNow;
        var activeVersions = await (from document in db.LegalDocuments
                                    join version in db.LegalDocumentVersions on document.Id equals version.LegalDocumentId
                                    where document.IsActive && (document.AudienceCode == "CUSTOMER" || document.AudienceCode == "ALL") && version.IsActive &&
                                          version.EffectiveFrom <= now && (version.EffectiveTo == null || version.EffectiveTo > now)
                                    orderby version.VersionNo descending
                                    select new { Document = document, Version = version }).ToListAsync(token);
        activeVersions = activeVersions.GroupBy(x => x.Document.Id).Select(x => x.First()).ToList();
        var consentMap = input.Consents.GroupBy(x => x.LegalDocumentVersionId).ToDictionary(x => x.Key, x => x.Last().Agreed);
        if (activeVersions.Any(x => x.Document.RequirementCode == "REQUIRED" && (!consentMap.TryGetValue(x.Version.PublicId, out var agreed) || !agreed)))
            throw Bad("REQUIRED_CONSENT_MISSING", "필수 약관에 동의해 주세요.");
        if (consentMap.Keys.Any(id => activeVersions.All(x => x.Version.PublicId != id))) throw Bad("LEGAL_VERSION_INVALID", "현재 사용할 수 없는 약관 버전입니다.");

        var role = await db.Roles.SingleOrDefaultAsync(x => x.Code == RoleCodes.Customer && x.IsActive, token)
            ?? throw new CustomerAccountException(500, "CUSTOMER_ROLE_UNAVAILABLE", "고객 역할을 사용할 수 없습니다.");
        IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(token) : null;
        try
        {
            var user = new User
            {
                LoginId = login, NormalizedLoginId = normalizedLogin, Email = email, NormalizedEmail = email?.ToUpperInvariant(), Phone = phone,
                EmailVerificationStatusCode = "NOT_INTEGRATED", PhoneVerificationStatusCode = externalVerificationUnavailable ? "NOT_INTEGRATED" : "VERIFIED", StatusCode = "ACTIVE",
                CreatedAt = now, UpdatedAt = now,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, input.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync(token);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = now });
            db.CustomerProfiles.Add(new CustomerProfile { UserId = user.Id, DisplayName = input.Name.Trim(), CreatedAt = now, UpdatedAt = now });
            foreach (var item in activeVersions.Where(x => consentMap.GetValueOrDefault(x.Version.PublicId)))
                db.UserConsents.Add(new UserConsent
                {
                    UserId = user.Id, LegalDocumentVersionId = item.Version.Id, ConsentStatusCode = "CONSENTED", ConsentedAt = now,
                    SourceCode = "WEB_SIGNUP", IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Trim(context.Request.Headers.UserAgent.ToString(), 1000), CreatedAt = now,
                });
            await db.SaveChangesAsync(token);
            if (transaction is not null) await transaction.CommitAsync(token);
            return new(user.PublicId, user.LoginId, [RoleCodes.Customer]);
        }
        catch (DbUpdateException)
        {
            if (transaction is not null) await transaction.RollbackAsync(token);
            throw Conflict("REGISTRATION_DUPLICATE", "이미 등록된 아이디, 휴대전화 또는 이메일입니다.");
        }
        finally { if (transaction is not null) await transaction.DisposeAsync(); }
    }

    public async Task<CustomerProfileResponse> Profile(ClaimsPrincipal principal, CancellationToken token)
    {
        var id = PublicUserId(principal);
        var row = await (from user in db.Users.AsNoTracking()
                      join profile in db.CustomerProfiles.AsNoTracking() on user.Id equals profile.UserId
                      where user.PublicId == id
                      select new { profile.DisplayName, User = user }).SingleOrDefaultAsync(token)
            ?? throw NotFound("CUSTOMER_PROFILE_NOT_FOUND", "고객 프로필을 찾을 수 없습니다.");
        return new(row.DisplayName, row.User.LoginId, personalDataReader.Read(row.User.EmailEncrypted, row.User.Email),
            FormatPhone(personalDataReader.Read(row.User.PhoneEncrypted, row.User.Phone)), row.User.EmailVerificationStatusCode,
            row.User.PhoneVerificationStatusCode, row.User.StatusCode, row.User.CreatedAt, row.User.LastLoginAt);
    }

    public async Task<CustomerProfileResponse> UpdateProfile(ClaimsPrincipal principal, UpdateCustomerProfileRequest input, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var email = string.IsNullOrWhiteSpace(input.Email) ? null : NormalizeEmail(input.Email).ToLowerInvariant();
        if (email is not null)
        {
            EnsureEmail(email);
            var normalized = email.ToUpperInvariant();
            var duplicate = searchHasher.IsConfigured
                ? await db.Users.AnyAsync(x => x.Id != identity.User.Id && (x.EmailSearchHash != null && x.EmailSearchHash.SequenceEqual(searchHasher.Email(email)) || x.EmailSearchHash == null && (x.NormalizedEmail == normalized || x.NormalizedEmail == null && x.Email != null && x.Email.ToUpper() == normalized)), token)
                : await db.Users.AnyAsync(x => x.Id != identity.User.Id && (x.NormalizedEmail == normalized || x.NormalizedEmail == null && x.Email != null && x.Email.ToUpper() == normalized), token);
            if (duplicate)
                throw Conflict("EMAIL_DUPLICATE", "이미 사용 중인 이메일입니다.");
        }
        var phone = string.IsNullOrWhiteSpace(input.Phone) ? null : NormalizePhone(input.Phone);
        identity.Profile.DisplayName = input.Name.Trim();
        identity.User.Email = email;
        identity.User.NormalizedEmail = email?.ToUpperInvariant();
        identity.User.Phone = phone;
        identity.User.EmailVerificationStatusCode = "NOT_INTEGRATED";
        identity.User.PhoneVerificationStatusCode = "NOT_INTEGRATED";
        identity.User.UpdatedAt = identity.Profile.UpdatedAt = DateTime.UtcNow;
        identity.User.UpdatedByUserId = identity.Profile.UpdatedByUserId = identity.User.Id;
        await db.SaveChangesAsync(token);
        return await Profile(principal, token);
    }

    public async Task<List<CustomerAddressResponse>> Addresses(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var rows = await (from address in db.CustomerAddresses.AsNoTracking()
                      join area in db.AdministrativeAreas.AsNoTracking() on address.AdministrativeAreaId equals area.Id into areaGroup
                      from area in areaGroup.DefaultIfEmpty()
                      where address.CustomerProfileId == identity.Profile.Id && address.IsActive
                      orderby address.IsDefault descending, address.CreatedAt descending
                      select new { Address = address, AreaId = area == null ? (Guid?)null : area.PublicId, AreaName = area == null ? null : area.AreaName }).ToListAsync(token);
        return rows.Select(row => new CustomerAddressResponse(row.Address.PublicId, row.Address.AddressName,
            personalDataReader.Read(row.Address.RecipientNameEncrypted, row.Address.RecipientName), row.Address.PostalCode,
            personalDataReader.Read(row.Address.RoadAddressEncrypted, row.Address.RoadAddress) ?? string.Empty,
            personalDataReader.Read(row.Address.DetailAddressEncrypted, row.Address.DetailAddress) ?? string.Empty,
            row.AreaId, row.AreaName, row.Address.Latitude, row.Address.Longitude, row.Address.IsDefault, Convert.ToBase64String(row.Address.RowVersion))).ToList();
    }

    public async Task<CustomerAddressResponse> CreateAddress(ClaimsPrincipal principal, SaveCustomerAddressRequest input, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var areaId = await AreaId(input.AdministrativeAreaId, token);
        var hasDefault = await db.CustomerAddresses.AnyAsync(x => x.CustomerProfileId == identity.Profile.Id && x.IsActive && x.IsDefault, token);
        var makeDefault = input.IsDefault || !hasDefault;
        if (makeDefault) await UnsetDefault(identity.Profile.Id, null, identity.User.Id, token);
        var now = DateTime.UtcNow;
        var item = new CustomerAddress
        {
            CustomerProfileId = identity.Profile.Id, AddressName = input.AddressName.Trim(), RecipientName = Clean(input.RecipientName),
            PostalCode = input.PostalCode.Trim(), RoadAddress = input.RoadAddress.Trim(), DetailAddress = input.DetailAddress.Trim(),
            AdministrativeAreaId = areaId, Latitude = input.Latitude, Longitude = input.Longitude, IsDefault = makeDefault, IsActive = true,
            CreatedAt = now, CreatedByUserId = identity.User.Id, UpdatedAt = now, UpdatedByUserId = identity.User.Id,
        };
        db.CustomerAddresses.Add(item);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException) { throw Conflict("DEFAULT_ADDRESS_CONFLICT", "기본주소가 동시에 변경되었습니다. 다시 확인해 주세요."); }
        return (await Addresses(principal, token)).Single(x => x.Id == item.PublicId);
    }

    public async Task<CustomerAddressResponse> UpdateAddress(ClaimsPrincipal principal, Guid id, SaveCustomerAddressRequest input, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var item = await db.CustomerAddresses.SingleOrDefaultAsync(x => x.PublicId == id && x.CustomerProfileId == identity.Profile.Id && x.IsActive, token)
            ?? throw NotFound("CUSTOMER_ADDRESS_NOT_FOUND", "주소를 찾을 수 없습니다.");
        ApplyConcurrency(item, input.ConcurrencyToken);
        if (item.IsDefault && !input.IsDefault)
            throw Bad("DEFAULT_ADDRESS_REQUIRED", "기본주소는 해제할 수 없습니다. 다른 주소를 기본주소로 설정해 주세요.");
        if (input.IsDefault) await UnsetDefault(identity.Profile.Id, item.Id, identity.User.Id, token);
        item.AddressName = input.AddressName.Trim(); item.RecipientName = Clean(input.RecipientName); item.PostalCode = input.PostalCode.Trim();
        item.RoadAddress = input.RoadAddress.Trim(); item.DetailAddress = input.DetailAddress.Trim(); item.AdministrativeAreaId = await AreaId(input.AdministrativeAreaId, token);
        item.Latitude = input.Latitude; item.Longitude = input.Longitude; item.IsDefault = input.IsDefault; item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = identity.User.Id;
        await SaveConcurrency(token);
        return (await Addresses(principal, token)).Single(x => x.Id == id);
    }

    public async Task DeleteAddress(ClaimsPrincipal principal, Guid id, string? concurrencyToken, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var item = await db.CustomerAddresses.SingleOrDefaultAsync(x => x.PublicId == id && x.CustomerProfileId == identity.Profile.Id && x.IsActive, token)
            ?? throw NotFound("CUSTOMER_ADDRESS_NOT_FOUND", "주소를 찾을 수 없습니다.");
        ApplyConcurrency(item, concurrencyToken);
        if (item.IsDefault)
            throw Bad("DEFAULT_ADDRESS_DELETE_BLOCKED", "기본주소는 삭제할 수 없습니다. 다른 주소를 기본주소로 설정한 후 삭제해 주세요.");
        item.IsActive = false; item.IsDefault = false; item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = identity.User.Id;
        await SaveConcurrency(token);
    }

    public async Task ChangePassword(ClaimsPrincipal principal, ChangePasswordRequest input, HttpContext context, CancellationToken token)
    {
        EnsurePassword(input.NewPassword, input.NewPasswordConfirmation);
        var identity = await Identity(principal, token);
        if (passwordHasher.VerifyHashedPassword(identity.User, identity.User.PasswordHash, input.CurrentPassword) == PasswordVerificationResult.Failed)
            throw Bad("CURRENT_PASSWORD_INVALID", "현재 비밀번호가 올바르지 않습니다.");
        identity.User.PasswordHash = passwordHasher.HashPassword(identity.User, input.NewPassword);
        identity.User.UpdatedAt = DateTime.UtcNow; identity.User.UpdatedByUserId = identity.User.Id;
        db.AuditLogs.Add(new AuditLog
        {
            OccurredAt = DateTime.UtcNow, ActorUserId = identity.User.Id, ActorRoleCode = RoleCodes.Customer,
            ActionCode = "CUSTOMER_PASSWORD_CHANGED", EntityType = "USER", EntityPublicId = identity.User.PublicId,
            ResultCode = "SUCCESS", IpAddress = context.Connection.RemoteIpAddress?.ToString(), UserAgent = Trim(context.Request.Headers.UserAgent.ToString(), 1000),
        });
        await db.SaveChangesAsync(token);
    }

    public async Task RequestPasswordReset(string identifier, CancellationToken token)
    {
        var normalized = identifier.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.StatusCode == "ACTIVE" && (x.NormalizedLoginId == normalized || x.NormalizedEmail == normalized || x.NormalizedEmail == null && x.Email != null && x.Email.ToUpper() == normalized), token);
        var tokenValue = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var now = DateTime.UtcNow;
        var request = new PasswordResetRequest
        {
            UserId = user?.Id, RequestedIdentifierHash = Hash(normalized), RequestedIdentifierMasked = MaskIdentifier(identifier),
            TokenHash = Hash(tokenValue), ExpiresAt = now.AddMinutes(30), DeliveryStatusCode = resetDelivery.StatusCode, CreatedAt = now,
        };
        db.PasswordResetRequests.Add(request);
        await db.SaveChangesAsync(token);
        if (user is not null) await resetDelivery.DeliverAsync(request.RequestedIdentifierMasked, tokenValue, token);
    }

    public async Task ConfirmPasswordReset(string tokenValue, string password, string confirmation, CancellationToken token)
    {
        EnsurePassword(password, confirmation);
        var hash = Hash(tokenValue.Trim());
        var now = DateTime.UtcNow;
        var request = await db.PasswordResetRequests.SingleOrDefaultAsync(x => x.TokenHash.SequenceEqual(hash), token);
        if (request is null || request.UserId is null || request.UsedAt is not null || request.ExpiresAt <= now)
            throw Bad("PASSWORD_RESET_TOKEN_INVALID", "재설정 요청이 유효하지 않거나 만료되었습니다.");
        var user = await db.Users.SingleAsync(x => x.Id == request.UserId, token);
        user.PasswordHash = passwordHasher.HashPassword(user, password); user.UpdatedAt = now; user.UpdatedByUserId = user.Id; request.UsedAt = now;
        await db.SaveChangesAsync(token);
    }

    public async Task<List<ConsentResponse>> Consents(ClaimsPrincipal principal, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var documents = await ActiveDocuments(token);
        var versionIds = documents.Select(x => x.VersionId).ToArray();
        var rows = await (from consent in db.UserConsents.AsNoTracking()
                          join version in db.LegalDocumentVersions.AsNoTracking() on consent.LegalDocumentVersionId equals version.Id
                          where consent.UserId == identity.User.Id && versionIds.Contains(version.PublicId)
                          select new { version.PublicId, consent.ConsentStatusCode, consent.ConsentedAt, consent.WithdrawnAt }).ToListAsync(token);
        return documents.Select(document =>
        {
            var row = rows.SingleOrDefault(x => x.PublicId == document.VersionId);
            return new ConsentResponse(document.VersionId, document.Code, document.RequirementCode, document.Title, document.Content, document.VersionNo,
                document.EffectiveFrom, document.EffectiveTo, document.IsPlaceholder, document.RequirementCode == "OPTIONAL",
                row?.ConsentStatusCode ?? "NOT_CONSENTED", row?.ConsentedAt, row?.WithdrawnAt);
        }).ToList();
    }

    public async Task UpdateConsent(ClaimsPrincipal principal, UpdateConsentRequest input, HttpContext context, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var now = DateTime.UtcNow;
        var row = await (from version in db.LegalDocumentVersions
                         join document in db.LegalDocuments on version.LegalDocumentId equals document.Id
                         where version.PublicId == input.LegalDocumentVersionId && version.IsActive && document.IsActive
                         select new { Version = version, Document = document }).SingleOrDefaultAsync(token)
            ?? throw NotFound("LEGAL_VERSION_NOT_FOUND", "약관 버전을 찾을 수 없습니다.");
        if (!input.Agreed && row.Document.RequirementCode == "REQUIRED") throw Bad("REQUIRED_CONSENT_WITHDRAWAL_BLOCKED", "필수 동의는 계정 이용 중 철회할 수 없습니다.");
        var consent = await db.UserConsents.SingleOrDefaultAsync(x => x.UserId == identity.User.Id && x.LegalDocumentVersionId == row.Version.Id, token);
        var previousStatus = consent?.ConsentStatusCode ?? "NOT_CONSENTED";
        var nextStatus = input.Agreed ? "CONSENTED" : "WITHDRAWN";
        if (consent is null)
        {
            if (!input.Agreed) return;
            db.UserConsents.Add(new UserConsent { UserId = identity.User.Id, LegalDocumentVersionId = row.Version.Id, ConsentStatusCode = "CONSENTED", ConsentedAt = now, SourceCode = "MY_SOODAL", IpAddress = context.Connection.RemoteIpAddress?.ToString(), UserAgent = Trim(context.Request.Headers.UserAgent.ToString(), 1000), CreatedAt = now });
        }
        else
        {
            consent.ConsentStatusCode = input.Agreed ? "CONSENTED" : "WITHDRAWN";
            consent.ConsentedAt = input.Agreed ? now : consent.ConsentedAt;
            consent.WithdrawnAt = input.Agreed ? null : now;
        }
        if (previousStatus != nextStatus)
            db.AuditLogs.Add(new AuditLog { OccurredAt = now, ActorUserId = identity.User.Id, ActorRoleCode = RoleCodes.Customer,
                ActionCode = input.Agreed ? "CUSTOMER_CONSENT_GRANTED" : "CUSTOMER_CONSENT_WITHDRAWN", EntityType = "LEGAL_DOCUMENT_VERSION",
                EntityPublicId = row.Version.PublicId, ResultCode = "SUCCESS", AfterJson = JsonSerializer.Serialize(new { consentStatus = nextStatus }) });
        await db.SaveChangesAsync(token);
    }

    public async Task<WithdrawalResponse> RequestWithdrawal(ClaimsPrincipal principal, CreateWithdrawalRequest input, CancellationToken token)
    {
        var identity = await Identity(principal, token);
        var scope = input.ScopeCode.Trim().ToUpperInvariant();
        if (scope is not ("CUSTOMER_ROLE" or "ACCOUNT")) throw Bad("WITHDRAWAL_SCOPE_INVALID", "탈퇴 신청 범위를 확인해 주세요.");
        var existing = await db.CustomerWithdrawalRequests.SingleOrDefaultAsync(x => x.UserId == identity.User.Id && x.ScopeCode == scope && x.StatusCode == "REQUESTED", token);
        if (existing is not null) return new(existing.PublicId, existing.ScopeCode, existing.StatusCode, existing.RequestedAt);
        var item = new CustomerWithdrawalRequest { UserId = identity.User.Id, ScopeCode = scope, StatusCode = "REQUESTED", Reason = Clean(input.Reason), RequestedAt = DateTime.UtcNow };
        db.CustomerWithdrawalRequests.Add(item); await db.SaveChangesAsync(token);
        return new(item.PublicId, item.ScopeCode, item.StatusCode, item.RequestedAt);
    }

    private async Task<(User User, CustomerProfile Profile)> Identity(ClaimsPrincipal principal, CancellationToken token)
    {
        var publicId = PublicUserId(principal);
        var row = await (from user in db.Users join profile in db.CustomerProfiles on user.Id equals profile.UserId
                         where user.PublicId == publicId && user.StatusCode == "ACTIVE" select new { user, profile }).SingleOrDefaultAsync(token);
        return row is null ? throw NotFound("CUSTOMER_PROFILE_NOT_FOUND", "고객 프로필을 찾을 수 없습니다.") : (row.user, row.profile);
    }

    private async Task<long?> AreaId(Guid? publicId, CancellationToken token)
    {
        if (!publicId.HasValue) return null;
        return await db.AdministrativeAreas.Where(x => x.PublicId == publicId && x.IsActive && x.AreaLevelCode == "SIGUNGU").Select(x => (long?)x.Id).SingleOrDefaultAsync(token)
            ?? throw Bad("ADMINISTRATIVE_AREA_INVALID", "사용할 수 없는 행정구역입니다.");
    }

    private async Task UnsetDefault(long profileId, long? exceptId, long actorId, CancellationToken token)
    {
        var values = await db.CustomerAddresses.Where(x => x.CustomerProfileId == profileId && x.IsActive && x.IsDefault && (!exceptId.HasValue || x.Id != exceptId)).ToListAsync(token);
        foreach (var value in values) { value.IsDefault = false; value.UpdatedAt = DateTime.UtcNow; value.UpdatedByUserId = actorId; }
    }

    private void ApplyConcurrency(CustomerAddress item, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        try { db.Entry(item).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(value); }
        catch (FormatException) { throw Bad("CONCURRENCY_TOKEN_INVALID", "주소 버전값이 올바르지 않습니다."); }
    }
    private async Task SaveConcurrency(CancellationToken token)
    {
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { throw Conflict("CUSTOMER_ADDRESS_CHANGED", "주소가 다른 곳에서 변경되었습니다. 다시 확인해 주세요."); }
        catch (DbUpdateException) { throw Conflict("DEFAULT_ADDRESS_CONFLICT", "기본주소가 동시에 변경되었습니다. 다시 확인해 주세요."); }
    }
    private static Guid PublicUserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new CustomerAccountException(401, "AUTHENTICATION_REQUIRED", "로그인이 필요합니다.");
    private static string NormalizeLogin(string value) => value.Trim().ToUpperInvariant();
    private static string NormalizeEmail(string value) => value.Trim().ToUpperInvariant();
    private static string NormalizePhone(string value) { var digits = new string(value.Where(char.IsDigit).ToArray()); if (!PhonePattern().IsMatch(digits)) throw Bad("PHONE_INVALID", "휴대전화 번호를 010-1234-5678 형식으로 입력해 주세요."); return digits; }
    private static string? FormatPhone(string? value) => value is { Length: 11 } ? $"{value[..3]}-{value[3..7]}-{value[7..]}" : value;
    private static void EnsureEmail(string value) { if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(value)) throw Bad("EMAIL_INVALID", "이메일 형식을 확인해 주세요."); }
    private static void EnsurePassword(string value, string confirmation)
    {
        if (value != confirmation) throw Bad("PASSWORD_CONFIRMATION_MISMATCH", "비밀번호를 다시 확인하세요.");
        if (!AccountPasswordPolicy.IsSatisfied(value))
            throw Bad("PASSWORD_POLICY_VIOLATION", "비밀번호가 규칙에 맞지 않습니다.");
    }
    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
    private static string MaskIdentifier(string value) { var text = value.Trim(); var at = text.IndexOf('@'); return at > 1 ? $"{text[0]}***{text[(at - 1)..]}" : text.Length > 3 ? $"{text[..2]}***{text[^1]}" : "***"; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? Trim(string? value, int length) => string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, length)];
    private static CustomerAccountException Bad(string code, string message) => new(400, code, message);
    private static CustomerAccountException Conflict(string code, string message) => new(409, code, message);
    private static CustomerAccountException NotFound(string code, string message) => new(404, code, message);
    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_-]{3,255}$", RegexOptions.CultureInvariant)] private static partial Regex LoginIdPattern();
    [GeneratedRegex("^010[0-9]{8}$", RegexOptions.CultureInvariant)] private static partial Regex PhonePattern();
}
