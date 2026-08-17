using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Trust;

public sealed class ProviderTrustService(SoodalLifeDbContext db)
{
    public async Task<ProviderTrustDashboardResponse> GetAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw Error("PROVIDER_IDENTITY_INVALID", "공급자 로그인 정보를 확인할 수 없습니다.", StatusCodes.Status401Unauthorized);

        var provider = await (from user in db.Users.AsNoTracking()
                              join roleLink in db.UserRoles.AsNoTracking() on user.Id equals roleLink.UserId
                              join role in db.Roles.AsNoTracking() on roleLink.RoleId equals role.Id
                              join profile in db.ProviderProfiles.AsNoTracking() on user.Id equals profile.UserId
                              where user.PublicId == userId && user.StatusCode == "ACTIVE" && role.Code == RoleCodes.Provider &&
                                    role.IsActive && roleLink.RevokedAt == null
                              select profile).SingleOrDefaultAsync(token)
            ?? throw Error("PROVIDER_TRUST_NOT_FOUND", "공급자 정보를 찾을 수 없습니다.", StatusCodes.Status404NotFound);

        var current = await db.ProviderTrustScoreCurrent.AsNoTracking().SingleOrDefaultAsync(x => x.ProviderProfileId == provider.Id, token);
        var score = current?.Score ?? provider.TrustScore;
        var evaluationStatus = current?.EvaluationStatusCode ?? (provider.TrustScore is null ? "NEW_OR_EVALUATING" : "LEGACY_UNKNOWN_POLICY");
        var policy = current?.TrustPolicyId is long currentPolicyId
            ? await db.TrustPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == currentPolicyId, token)
            : await db.TrustPolicies.AsNoTracking().Where(x => x.StatusCode == "ACTIVE").OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(token);

        var events = await (from item in db.TrustScoreEvents.AsNoTracking()
                            join eventPolicy in db.TrustPolicies.AsNoTracking() on item.TrustPolicyId equals eventPolicy.Id into policies
                            from eventPolicy in policies.DefaultIfEmpty()
                            where item.ProviderProfileId == provider.Id
                            orderby item.OccurredAt descending, item.Id descending
                            select new { Item = item, PolicyVersion = eventPolicy == null ? null : eventPolicy.PolicyVersion })
            .Take(100).ToListAsync(token);

        return new(provider.PublicId, score, GradeCode(score), GradeLabel(score), evaluationStatus, StatusNotice(evaluationStatus, policy?.PolicyVersion),
            policy?.PolicyVersion, current?.CalculatedAt, Grades, ReadComponents(policy?.RulesJson),
            events.Select(x => new ProviderTrustEventResponse(x.Item.PublicId, x.Item.OccurredAt, x.Item.EventTypeCode,
                EventLabel(x.Item.EventTypeCode), x.Item.ScoreBefore, x.Item.ScoreDelta, x.Item.ScoreAfter,
                x.Item.GradeBefore is null ? null : GradeLabel(x.Item.GradeBefore), x.Item.GradeAfter is null ? null : GradeLabel(x.Item.GradeAfter),
                EventReason(x.Item.EventTypeCode, x.Item.ReasonText), x.PolicyVersion)).ToArray());
    }

    private static readonly ProviderTrustGradeResponse[] Grades =
    [
        new("SPROUT", "새싹수달", 0, 59.9999m, "신뢰도 평가가 시작된 단계입니다."),
        new("SAFE", "안심수달", 60, 69.9999m, "기본적인 거래 신뢰 기준을 충족한 단계입니다."),
        new("TRUSTED", "믿음수달", 70, 79.9999m, "안정적인 서비스 이력이 쌓인 단계입니다."),
        new("EXCELLENT", "우수수달", 80, 89.9999m, "우수한 거래와 고객 평가를 유지한 단계입니다."),
        new("HONOR", "명예수달", 90, 100, "최상위 신뢰 기준을 충족한 단계입니다.")
    ];

    private static IReadOnlyList<ProviderTrustPolicyComponentResponse> ReadComponents(string? rulesJson)
    {
        if (string.IsNullOrWhiteSpace(rulesJson)) return [];
        try
        {
            using var document = JsonDocument.Parse(rulesJson);
            if (!document.RootElement.TryGetProperty("components", out var components)) return [];
            return components.EnumerateArray().Select(item =>
            {
                var code = item.GetProperty("code").GetString() ?? string.Empty;
                var weight = item.TryGetProperty("weight", out var value) ? value.GetDecimal() : 0;
                return new ProviderTrustPolicyComponentResponse(code, ComponentName(code), weight, ComponentDescription(code));
            }).ToArray();
        }
        catch (JsonException) { return []; }
    }

    private static string GradeCode(decimal? score) => score switch { null => "NEW", < 60 => "SPROUT", < 70 => "SAFE", < 80 => "TRUSTED", < 90 => "EXCELLENT", <= 100 => "HONOR", _ => "UNKNOWN" };
    private static string GradeLabel(decimal? score) => GradeLabel(GradeCode(score));
    private static string GradeLabel(string code) => code switch { "SPROUT" => "새싹수달", "SAFE" => "안심수달", "TRUSTED" => "믿음수달", "EXCELLENT" => "우수수달", "HONOR" => "명예수달", "NEW" => "신규·평가중", _ => "점수 확인필요" };
    private static string StatusNotice(string status, string? policyVersion) => status switch
    {
        "NEW_OR_EVALUATING" => "거래와 검증된 리뷰가 쌓이면 신뢰도 점수가 산정됩니다.",
        "LEGACY_UNKNOWN_POLICY" => "기존 점수의 적용 정책을 확인하고 있습니다.",
        _ => policyVersion is null ? "신뢰도 산정 정책을 확인하고 있습니다." : $"본사 신뢰도 정책 {policyVersion} 기준으로 산정된 점수입니다."
    };
    private static string EventLabel(string code) => code switch { "AUTOMATIC_CALCULATION" => "정기 신뢰도 산정", "MANUAL_ADJUSTMENT" => "관리자 조정", "RESTORE" => "점수 복원", _ => "신뢰도 변경" };
    private static string EventReason(string code, string? reason) => !string.IsNullOrWhiteSpace(reason) && !reason.Contains("ACTIVE Trust", StringComparison.OrdinalIgnoreCase)
        ? reason : code == "AUTOMATIC_CALCULATION" ? "거래·고객평가·증빙 등 현재 정책 항목을 반영해 자동 산정되었습니다." : "신뢰도 점수가 변경되었습니다.";
    private static string ComponentName(string code) => code switch { "EVIDENCE" => "인증·증빙", "TRANSACTION" => "거래 이행", "REVIEW" => "고객 평가", "AFTER_SERVICE" => "A/S 처리", "DISPUTE" => "분쟁 처리", "SANCTION" => "운영 정책 준수", _ => code };
    private static string ComponentDescription(string code) => code switch { "EVIDENCE" => "필수 서류의 승인·유효 상태", "TRANSACTION" => "거래 완료와 완료 증빙 이력", "REVIEW" => "검증된 거래의 공개 고객 평가", "AFTER_SERVICE" => "A/S 해결과 재발 여부", "DISPUTE" => "구조화된 분쟁 판정 결과", "SANCTION" => "확정된 제재와 정책 준수 상태", _ => "현재 본사 정책에 포함된 평가 항목" };
    private static ProviderTrustException Error(string code, string message, int status) => new(code, message, status);
}
