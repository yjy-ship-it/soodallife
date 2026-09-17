using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Authentication;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class DataRetentionService(SoodalLifeDbContext db,AdminSecurityService security)
{
    public async Task UpdateAsync(Guid actorPublicId,Guid id,string reauth,AdminRetentionPolicyUpdateRequest input,CancellationToken token)
    {
        var actor=await security.RequireSensitiveAccessAsync(actorPublicId,reauth,AdminDetailRoles.SuperAdmin,AdminDetailRoles.Security);
        if(input.ActionCode is not("DELETE" or "ANONYMIZE")||input.RetentionDays is<1 or>3650||input.LegalHoldDays is<0 or>3650)throw new AdminSystemException("RETENTION_POLICY_INVALID","보유기간·법적 보존기간·처리방식을 확인해 주세요.");
        if(input.IsEnabled&&!input.DryRun)throw new AdminSystemException("RETENTION_DESTRUCTIVE_APPROVAL_REQUIRED","실제 파기 활성화는 법정 보존기간 확정과 별도 운영 승인이 필요합니다.",409);
        var row=await db.DataRetentionPolicies.SingleOrDefaultAsync(x=>x.PublicId==id,token)??throw new AdminSystemException("RETENTION_POLICY_NOT_FOUND","개인정보 보존 정책을 찾을 수 없습니다.",404);var before=new{row.ActionCode,row.RetentionDays,row.LegalHoldDays,row.IsEnabled,row.DryRun};
        row.ActionCode=input.ActionCode;row.RetentionDays=input.RetentionDays;row.LegalHoldDays=input.LegalHoldDays;row.IsEnabled=input.IsEnabled;row.DryRun=true;row.UpdatedAt=DateTime.UtcNow;row.UpdatedByUserId=actor;
        db.AuditLogs.Add(new(){OccurredAt=DateTime.UtcNow,ActorUserId=actor,ActorRoleCode=RoleCodes.Admin,ActionCode="RETENTION_POLICY_UPDATED",EntityType="DATA_RETENTION_POLICY",EntityPublicId=row.PublicId,ResultCode="SUCCESS",BeforeJson=System.Text.Json.JsonSerializer.Serialize(before),AfterJson=System.Text.Json.JsonSerializer.Serialize(new{row.ActionCode,row.RetentionDays,row.LegalHoldDays,row.IsEnabled,row.DryRun})});await db.SaveChangesAsync(token);
    }

    public async Task<AdminRetentionPolicyItem> RunAssessmentAsync(Guid actorPublicId,Guid id,string reauth,CancellationToken token)
    {
        var actor=await security.RequireSensitiveAccessAsync(actorPublicId,reauth,AdminDetailRoles.SuperAdmin,AdminDetailRoles.Security);var policy=await db.DataRetentionPolicies.SingleOrDefaultAsync(x=>x.PublicId==id,token)??throw new AdminSystemException("RETENTION_POLICY_NOT_FOUND","개인정보 보존 정책을 찾을 수 없습니다.",404);await AssessAsync(policy,actor,token);return Item(policy);
    }

    public async Task<int> AssessEnabledAsync(CancellationToken token){var policies=await db.DataRetentionPolicies.Where(x=>x.IsEnabled).ToListAsync(token);var count=0;foreach(var policy in policies)count+=(await AssessAsync(policy,null,token)).CandidateCount;return count;}

    private async Task<DataRetentionExecution> AssessAsync(DataRetentionPolicy policy,long? actor,CancellationToken token)
    {
        var cutoff=DateTime.UtcNow.AddDays(-policy.RetentionDays);var execution=new DataRetentionExecution{PolicyId=policy.Id,StatusCode="RUNNING",DryRun=true,StartedAt=DateTime.UtcNow,ExecutedByUserId=actor};db.DataRetentionExecutions.Add(execution);
        if(policy.DomainCode=="PASSWORD_RESET")execution.CandidateCount=await db.PasswordResetRequests.AsNoTracking().CountAsync(x=>x.ExpiresAt<cutoff,token);
        else if(policy.DomainCode=="ADMIN_SESSION")execution.CandidateCount=await db.AdminReauthenticationSessions.AsNoTracking().CountAsync(x=>x.ExpiresAt<cutoff,token);
        else if(policy.DomainCode=="ANALYTICS_EVENT")execution.CandidateCount=await db.AnalyticsEvents.AsNoTracking().CountAsync(x=>x.OccurredAt<cutoff,token);
        else if(policy.DomainCode=="PERSONAL_DATA")
        {
            var ids=await db.Users.AsNoTracking().Where(x=>x.StatusCode=="WITHDRAWN"&&x.UpdatedAt<cutoff).Select(x=>x.Id).ToListAsync(token);execution.CandidateCount=ids.Count;
            execution.SkippedLegalHoldCount=await db.DisputeCases.AsNoTracking().Where(x=>ids.Contains(x.ApplicantUserId)||ids.Contains(x.CounterpartyUserId)).Where(x=>x.StatusCode!="CLOSED").Select(x=>x.ApplicantUserId).Distinct().CountAsync(token);
        }
        execution.StatusCode="SUCCEEDED";execution.CompletedAt=DateTime.UtcNow;await db.SaveChangesAsync(token);return execution;
    }
    private static AdminRetentionPolicyItem Item(DataRetentionPolicy x)=>new(x.PublicId,x.DomainCode,x.ActionCode,x.RetentionDays,x.LegalHoldDays,x.IsEnabled,x.DryRun,x.UpdatedAt);
}
