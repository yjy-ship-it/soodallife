using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class TrustReferenceDataInitializer(SoodalLifeDbContext db, ILogger<TrustReferenceDataInitializer> logger)
{
    private static readonly (string Code,string Name,string Description,int Order)[] RatingItems =
    [
        ("SERVICE_QUALITY","작업 품질","요청한 작업의 완성도와 결과 품질을 평가합니다.",10),
        ("PUNCTUALITY","약속·시간 준수","방문·작업 일정과 약속 시간을 지켰는지 평가합니다.",20),
        ("COMMUNICATION","설명·소통","진행 과정과 필요한 내용을 이해하기 쉽게 안내했는지 평가합니다.",30),
        ("COURTESY","친절·응대","고객을 존중하고 친절하게 응대했는지 평가합니다.",40),
        ("PRICE_TRANSPARENCY","가격·추가비용 안내","견적과 추가비용을 사전에 투명하게 안내했는지 평가합니다.",50),
        ("CLEANUP","정리·마무리","작업 후 현장 정리와 마무리가 적절했는지 평가합니다.",60)
    ];
    private static readonly (string Code,string Name,string Description,int Order)[] Liabilities =
    [
        ("NO_PROVIDER_LIABILITY","전문가 귀책 없음","전문가 책임이 없는 것으로 최종 판정된 경우",10),
        ("CUSTOMER_LIABILITY","고객 귀책","고객 책임으로 최종 판정된 경우",20),
        ("MUTUAL_MINOR","쌍방 경미 귀책","양측에 경미한 책임이 있는 경우",30),
        ("PROVIDER_PARTIAL","전문가 일부 귀책","전문가에게 일부 책임이 확정된 경우",40),
        ("PROVIDER_FULL","전문가 전부 귀책","전문가에게 전적인 책임이 확정된 경우",50)
    ];
    private static readonly (string Code,string Name,string Description,int Order)[] Sanctions =
    [
        ("NOTICE","안내·주의","경미한 위반에 대한 공식 안내 또는 주의",10),
        ("FORMAL_WARNING","경고","재발 방지를 요구하는 공식 경고",20),
        ("SERVICE_RESTRICTION","서비스 제한","일부 서비스 또는 기능 이용 제한",30),
        ("SUSPENSION","활동 정지","일정 기간 전문가 활동 정지",40)
    ];

    public async Task InitializeAsync(CancellationToken token = default)
    {
        await using var transaction=db.Database.IsRelational()?await db.Database.BeginTransactionAsync(token):null;
        try
        {
            if(db.Database.IsSqlServer())
            {
                foreach(var item in RatingItems)await db.Database.ExecuteSqlInterpolatedAsync($"""IF NOT EXISTS (SELECT 1 FROM review_rating_items WHERE UPPER(code)=UPPER({item.Code})) INSERT INTO review_rating_items (public_id,code,name,description,min_value,max_value,display_order,is_required,is_active,effective_from,created_at,updated_at) VALUES (NEWID(),{item.Code},{item.Name},{item.Description},1,5,{item.Order},1,1,SYSUTCDATETIME(),SYSUTCDATETIME(),SYSUTCDATETIME())""",token);
                foreach(var item in Liabilities)await db.Database.ExecuteSqlInterpolatedAsync($"""IF NOT EXISTS (SELECT 1 FROM dispute_liability_types WHERE UPPER(code)=UPPER({item.Code})) INSERT INTO dispute_liability_types (public_id,code,name,description,is_active,display_order,created_at,updated_at) VALUES (NEWID(),{item.Code},{item.Name},{item.Description},1,{item.Order},SYSUTCDATETIME(),SYSUTCDATETIME())""",token);
                foreach(var item in Sanctions)await db.Database.ExecuteSqlInterpolatedAsync($"""IF NOT EXISTS (SELECT 1 FROM sanction_types WHERE UPPER(code)=UPPER({item.Code})) INSERT INTO sanction_types (public_id,code,name,description,is_active,display_order,effective_from,created_at,updated_at) VALUES (NEWID(),{item.Code},{item.Name},{item.Description},1,{item.Order},SYSUTCDATETIME(),SYSUTCDATETIME(),SYSUTCDATETIME())""",token);
            }
            else
            {
                var now=DateTime.UtcNow;var ratingCodes=(await db.ReviewRatingItems.Select(x=>x.Code).ToListAsync(token)).ToHashSet(StringComparer.OrdinalIgnoreCase);foreach(var item in RatingItems.Where(x=>!ratingCodes.Contains(x.Code)))db.ReviewRatingItems.Add(new ReviewRatingItem{Code=item.Code,Name=item.Name,Description=item.Description,MinValue=1,MaxValue=5,DisplayOrder=item.Order,IsRequired=true,IsActive=true,EffectiveFrom=now,CreatedAt=now,UpdatedAt=now});var liabilityCodes=(await db.DisputeLiabilityTypes.Select(x=>x.Code).ToListAsync(token)).ToHashSet(StringComparer.OrdinalIgnoreCase);foreach(var item in Liabilities.Where(x=>!liabilityCodes.Contains(x.Code)))db.DisputeLiabilityTypes.Add(new DisputeLiabilityType{Code=item.Code,Name=item.Name,Description=item.Description,DisplayOrder=item.Order,IsActive=true,CreatedAt=now,UpdatedAt=now});var sanctionCodes=(await db.SanctionTypes.Select(x=>x.Code).ToListAsync(token)).ToHashSet(StringComparer.OrdinalIgnoreCase);foreach(var item in Sanctions.Where(x=>!sanctionCodes.Contains(x.Code)))db.SanctionTypes.Add(new SanctionType{Code=item.Code,Name=item.Name,Description=item.Description,DisplayOrder=item.Order,IsActive=true,EffectiveFrom=now,CreatedAt=now,UpdatedAt=now});await db.SaveChangesAsync(token);
            }
            var draft=await db.TrustPolicies.SingleOrDefaultAsync(x=>x.PolicyVersion=="v1.0-draft"&&x.StatusCode=="DRAFT",token);
            if(draft is not null&&PatchDraft(draft))
            {
                var rulesJson=draft.RulesJson;var updatedAt=DateTime.UtcNow;
                if(db.Database.IsSqlServer())
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE trust_policies SET rules_json={rulesJson}, updated_at={updatedAt} WHERE id={draft.Id} AND status_code='DRAFT'",token);
                    db.Entry(draft).State=EntityState.Detached;
                }
                else
                {
                    draft.UpdatedAt=updatedAt;await db.SaveChangesAsync(token);
                }
            }
            if(transaction is not null)await transaction.CommitAsync(token);logger.LogInformation("Trust reference data initialization completed explicitly.");
        }
        catch{if(transaction is not null)await transaction.RollbackAsync(token);throw;}
    }

    private static bool PatchDraft(TrustPolicy draft)
    {
        var root=JsonNode.Parse(draft.RulesJson)?.AsObject(); var components=root?["components"]?.AsArray(); if(components is null)return false;
        var defaults=JsonNode.Parse(TrustPolicyDraftDefaults.RulesJson)!["components"]!.AsArray(); var changed=false;
        foreach(var component in components)
        {
            var code=component?["code"]?.GetValue<string>(); var settings=component?["settings"]?.AsObject(); var source=defaults.FirstOrDefault(x=>x?["code"]?.GetValue<string>()==code)?["settings"]?.AsObject();
            if(settings is null||source is null)continue;
            foreach(var pair in source) if(!settings.ContainsKey(pair.Key)||(settings[pair.Key] is JsonObject current&&current.Count==0)) { settings[pair.Key]=pair.Value?.DeepClone(); changed=true; }
        }
        if(changed)draft.RulesJson=root!.ToJsonString(); return changed;
    }
}
