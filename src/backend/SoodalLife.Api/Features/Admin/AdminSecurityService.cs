using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public static class AdminDetailRoles
{
    public const string SuperAdmin="SUPER_ADMIN", Operations="OPERATIONS", Finance="FINANCE", CustomerSupport="CUSTOMER_SUPPORT", Content="CONTENT", Analyst="ANALYST", Security="SECURITY";
    public static readonly string[] All=[SuperAdmin,Operations,Finance,CustomerSupport,Content,Analyst,Security];
}

public sealed record AdminMfaEnrollmentResponse(string Secret,string OtpAuthUri);
public sealed record AdminMfaConfirmRequest(string Code);
public sealed record AdminReauthenticateRequest(string Password,string? MfaCode,string Purpose="SENSITIVE_ADMIN_ACTION");
public sealed record AdminReauthenticateResponse(string Token,DateTime ExpiresAt);
public sealed record AdminAccountCreateRequest(string LoginId,string Password,string DetailRoleCode);
public sealed record AdminAccountUpdateRequest(string StatusCode,string DetailRoleCode);
public sealed record AdminSecurityAccountResponse(Guid Id,string LoginId,string Status,string DetailRoleCode,bool MfaEnabled,DateTime? MfaConfirmedAt,DateTime CreatedAt,DateTime? LastLoginAt);

public interface IAdminMfaVerifier { Task<bool> VerifyForLoginAsync(long userId,string? code,CancellationToken token); }

public sealed class AdminSecurityService(SoodalLifeDbContext db,IPasswordHasher<User> passwordHasher,IDataProtectionProvider protectionProvider) : IAdminMfaVerifier
{
    private readonly IDataProtector protector=protectionProvider.CreateProtector("SoodalLife.AdminMfa.v1");

    public async Task<bool> VerifyForLoginAsync(long userId,string? code,CancellationToken token)
    {
        var profile=await db.AdminSecurityProfiles.SingleOrDefaultAsync(x=>x.UserId==userId,token);
        if(profile is null||!profile.MfaEnabled)return true;
        var now=DateTime.UtcNow;if(profile.LockedUntil>now)return false;
        var valid=!string.IsNullOrWhiteSpace(code)&&VerifyTotp(Unprotect(profile.TotpSecretProtected),code!,now);
        if(valid){profile.FailedMfaAttempts=0;profile.LockedUntil=null;}
        else{profile.FailedMfaAttempts++;if(profile.FailedMfaAttempts>=5){profile.LockedUntil=now.AddMinutes(15);profile.FailedMfaAttempts=0;}}
        profile.UpdatedAt=now;await db.SaveChangesAsync(token);return valid;
    }

    public async Task<AdminMfaEnrollmentResponse> BeginMfaAsync(Guid actorPublicId,CancellationToken token)
    {
        var (user,profile)=await Actor(actorPublicId,token);var secret=Base32(RandomNumberGenerator.GetBytes(20));
        profile.TotpSecretProtected=protector.Protect(secret);profile.MfaEnabled=false;profile.MfaConfirmedAt=null;profile.UpdatedAt=DateTime.UtcNow;profile.UpdatedByUserId=user.Id;
        await db.SaveChangesAsync(token);var issuer=Uri.EscapeDataString("Soodal Life Admin");var account=Uri.EscapeDataString(user.LoginId);
        return new(secret,$"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&digits=6&period=30");
    }

    public async Task ConfirmMfaAsync(Guid actorPublicId,string code,CancellationToken token)
    {
        var (user,profile)=await Actor(actorPublicId,token);if(!VerifyTotp(Unprotect(profile.TotpSecretProtected),code,DateTime.UtcNow))throw Error("MFA_CODE_INVALID","인증 앱의 6자리 코드를 확인해 주세요.");
        profile.MfaEnabled=true;profile.MfaConfirmedAt=DateTime.UtcNow;profile.FailedMfaAttempts=0;profile.UpdatedAt=DateTime.UtcNow;profile.UpdatedByUserId=user.Id;
        Audit(user.Id,"ADMIN_MFA_ENABLED",user.PublicId,null,new{enabled=true});await db.SaveChangesAsync(token);
    }

    public async Task<AdminReauthenticateResponse> ReauthenticateAsync(Guid actorPublicId,AdminReauthenticateRequest request,CancellationToken token)
    {
        var (user,profile)=await Actor(actorPublicId,token);if(passwordHasher.VerifyHashedPassword(user,user.PasswordHash,request.Password)==PasswordVerificationResult.Failed)throw Error("REAUTHENTICATION_FAILED","비밀번호를 확인해 주세요.",401);
        if(profile.MfaEnabled&&!VerifyTotp(Unprotect(profile.TotpSecretProtected),request.MfaCode??"",DateTime.UtcNow))throw Error("MFA_CODE_INVALID","관리자 MFA 코드를 확인해 주세요.",401);
        var raw=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));var expires=DateTime.UtcNow.AddMinutes(10);
        db.AdminReauthenticationSessions.Add(new(){UserId=user.Id,TokenHash=SHA256.HashData(Encoding.UTF8.GetBytes(raw)),CreatedAt=DateTime.UtcNow,ExpiresAt=expires,PurposeCode=CleanPurpose(request.Purpose)});
        Audit(user.Id,"ADMIN_REAUTHENTICATED",user.PublicId,null,new{expiresAt=expires,purpose=CleanPurpose(request.Purpose)});await db.SaveChangesAsync(token);return new(raw,expires);
    }

    public async Task<long> RequireSensitiveAccessAsync(Guid actorPublicId,string? rawToken,params string[] allowedRoles)
    {
        var (user,profile)=await Actor(actorPublicId,CancellationToken.None);if(allowedRoles.Length>0&&!allowedRoles.Contains(profile.DetailRoleCode))throw Error("ADMIN_DETAIL_ROLE_REQUIRED","이 작업을 수행할 세부 관리자 권한이 없습니다.",403);
        if(string.IsNullOrWhiteSpace(rawToken))throw Error("ADMIN_REAUTHENTICATION_REQUIRED","중요 작업 전에 본인 재확인이 필요합니다.",428);
        var hash=SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));var now=DateTime.UtcNow;
        var session=await db.AdminReauthenticationSessions.SingleOrDefaultAsync(x=>x.UserId==user.Id&&x.RevokedAt==null&&x.ExpiresAt>now&&x.TokenHash.SequenceEqual(hash));
        if(session is null)throw Error("ADMIN_REAUTHENTICATION_REQUIRED","재확인 시간이 만료되었습니다. 다시 확인해 주세요.",428);return user.Id;
    }

    public async Task<IReadOnlyList<AdminSecurityAccountResponse>> AccountsAsync(CancellationToken token)=>await(from user in db.Users.AsNoTracking() join link in db.UserRoles.AsNoTracking() on user.Id equals link.UserId join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id join profile0 in db.AdminSecurityProfiles.AsNoTracking() on user.Id equals profile0.UserId into profiles from profile in profiles.DefaultIfEmpty() where role.Code==RoleCodes.Admin&&link.RevokedAt==null orderby user.LoginId select new AdminSecurityAccountResponse(user.PublicId,user.LoginId,user.StatusCode,profile==null?AdminDetailRoles.Operations:profile.DetailRoleCode,profile!=null&&profile.MfaEnabled,profile==null?null:profile.MfaConfirmedAt,user.CreatedAt,user.LastLoginAt)).ToListAsync(token);

    public async Task<AdminSecurityAccountResponse> CreateAccountAsync(Guid actorPublicId,string reauth,AdminAccountCreateRequest request,CancellationToken token)
    {
        var actor=await RequireSensitiveAccessAsync(actorPublicId,reauth,AdminDetailRoles.SuperAdmin);var login=request.LoginId.Trim();if(login.Length<4||request.Password.Length<12)throw Error("ADMIN_ACCOUNT_INPUT_INVALID","아이디는 4자 이상, 임시 비밀번호는 12자 이상이어야 합니다.");
        if(!AdminDetailRoles.All.Contains(request.DetailRoleCode))throw Error("ADMIN_DETAIL_ROLE_INVALID","관리자 세부 역할을 확인해 주세요.");var normalized=login.ToUpperInvariant();if(await db.Users.AnyAsync(x=>x.NormalizedLoginId==normalized,token))throw Error("LOGIN_ID_DUPLICATED","이미 사용 중인 아이디입니다.",409);
        var now=DateTime.UtcNow;var user=new User{LoginId=login,NormalizedLoginId=normalized,StatusCode="ACTIVE",CreatedAt=now,UpdatedAt=now,CreatedByUserId=actor,UpdatedByUserId=actor};user.PasswordHash=passwordHasher.HashPassword(user,request.Password);db.Users.Add(user);await db.SaveChangesAsync(token);
        var adminRole=await db.Roles.SingleAsync(x=>x.Code==RoleCodes.Admin,token);db.UserRoles.Add(new(){UserId=user.Id,RoleId=adminRole.Id,GrantedAt=now,GrantedByUserId=actor});db.AdminSecurityProfiles.Add(new(){UserId=user.Id,DetailRoleCode=request.DetailRoleCode,CreatedAt=now,UpdatedAt=now,CreatedByUserId=actor,UpdatedByUserId=actor});
        Audit(actor,"ADMIN_ACCOUNT_CREATED",user.PublicId,null,new{user.LoginId,request.DetailRoleCode});await db.SaveChangesAsync(token);return new(user.PublicId,user.LoginId,user.StatusCode,request.DetailRoleCode,false,null,user.CreatedAt,null);
    }

    public async Task UpdateAccountAsync(Guid actorPublicId,Guid targetId,string reauth,AdminAccountUpdateRequest request,CancellationToken token)
    {
        var actor=await RequireSensitiveAccessAsync(actorPublicId,reauth,AdminDetailRoles.SuperAdmin);if(!AdminDetailRoles.All.Contains(request.DetailRoleCode)||request.StatusCode is not("ACTIVE" or "SUSPENDED"))throw Error("ADMIN_ACCOUNT_INPUT_INVALID","상태 또는 관리자 세부 역할을 확인해 주세요.");
        var user=await db.Users.SingleOrDefaultAsync(x=>x.PublicId==targetId,token)??throw Error("ADMIN_NOT_FOUND","관리자 계정을 찾을 수 없습니다.",404);if(user.Id==actor&&request.StatusCode!="ACTIVE")throw Error("SELF_SUSPENSION_FORBIDDEN","현재 로그인한 관리자 계정은 정지할 수 없습니다.",409);
        var profile=await db.AdminSecurityProfiles.SingleOrDefaultAsync(x=>x.UserId==user.Id,token)??new AdminSecurityProfile{UserId=user.Id,CreatedAt=DateTime.UtcNow,CreatedByUserId=actor};if(profile.Id==0)db.AdminSecurityProfiles.Add(profile);
        var before=new{user.StatusCode,profile.DetailRoleCode};user.StatusCode=request.StatusCode;user.UpdatedAt=DateTime.UtcNow;user.UpdatedByUserId=actor;profile.DetailRoleCode=request.DetailRoleCode;profile.UpdatedAt=DateTime.UtcNow;profile.UpdatedByUserId=actor;
        if(request.StatusCode=="SUSPENDED"){var sessions=await db.AdminReauthenticationSessions.Where(x=>x.UserId==user.Id&&x.RevokedAt==null).ToListAsync(token);foreach(var item in sessions)item.RevokedAt=DateTime.UtcNow;}
        Audit(actor,"ADMIN_ACCOUNT_UPDATED",user.PublicId,before,new{user.StatusCode,profile.DetailRoleCode});await db.SaveChangesAsync(token);
    }

    private async Task<(User,AdminSecurityProfile)> Actor(Guid publicId,CancellationToken token)
    {
        var user=await(from u in db.Users join link in db.UserRoles on u.Id equals link.UserId join role in db.Roles on link.RoleId equals role.Id where u.PublicId==publicId&&u.StatusCode=="ACTIVE"&&link.RevokedAt==null&&role.Code==RoleCodes.Admin select u).SingleOrDefaultAsync(token)??throw Error("ADMIN_REQUIRED","관리자 권한이 필요합니다.",403);
        var profile=await db.AdminSecurityProfiles.SingleOrDefaultAsync(x=>x.UserId==user.Id,token);if(profile is null)throw Error("ADMIN_SECURITY_PROFILE_REQUIRED","관리자 보안 프로필을 먼저 적용해 주세요.",403);return(user,profile);
    }
    private string? Unprotect(string? value){if(string.IsNullOrWhiteSpace(value))return null;try{return protector.Unprotect(value);}catch(CryptographicException){return null;}}
    private static bool VerifyTotp(string? secret,string code,DateTime now){if(secret is null||code.Length!=6||!code.All(char.IsDigit))return false;var key=FromBase32(secret);var counter=new DateTimeOffset(now).ToUnixTimeSeconds()/30;var msg=new byte[8];for(var drift=-1;drift<=1;drift++){System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(msg,counter+drift);var hash=HMACSHA1.HashData(key,msg);var offset=hash[^1]&15;var value=((hash[offset]&127)<<24|(hash[offset+1]&255)<<16|(hash[offset+2]&255)<<8|(hash[offset+3]&255))%1_000_000;if(value.ToString("D6")==code)return true;}return false;}
    private static string Base32(byte[] data){const string alphabet="ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";var result=new StringBuilder();var buffer=0;var bits=0;foreach(var b in data){buffer=(buffer<<8)|b;bits+=8;while(bits>=5){result.Append(alphabet[(buffer>>(bits-5))&31]);bits-=5;}}if(bits>0)result.Append(alphabet[(buffer<<(5-bits))&31]);return result.ToString();}
    private static byte[] FromBase32(string value){const string alphabet="ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";var output=new List<byte>();var buffer=0;var bits=0;foreach(var c in value.ToUpperInvariant()){var index=alphabet.IndexOf(c);if(index<0)continue;buffer=(buffer<<5)|index;bits+=5;if(bits>=8){output.Add((byte)(buffer>>(bits-8)));bits-=8;}}return output.ToArray();}
    private static string CleanPurpose(string value)=>string.IsNullOrWhiteSpace(value)?"SENSITIVE_ADMIN_ACTION":value.Trim().ToUpperInvariant()[..Math.Min(50,value.Trim().Length)];
    private void Audit(long actor,string action,Guid target,object? before,object? after)=>db.AuditLogs.Add(new(){OccurredAt=DateTime.UtcNow,ActorUserId=actor,ActorRoleCode=RoleCodes.Admin,ActionCode=action,EntityType="ADMIN_SECURITY",EntityPublicId=target,ResultCode="SUCCESS",BeforeJson=before is null?null:System.Text.Json.JsonSerializer.Serialize(before),AfterJson=after is null?null:System.Text.Json.JsonSerializer.Serialize(after)});
    private static AdminSystemException Error(string code,string message,int status=400)=>new(code,message,status);
}
