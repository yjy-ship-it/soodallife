using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.Admin;

public sealed class TrustReferenceDataService(SoodalLifeDbContext db)
{
    public async Task<TrustReferenceDataResponse> GetAsync(CancellationToken token)
    {
        var ratings=await db.ReviewRatingItems.AsNoTracking().OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Id).Select(x=>new TrustRatingItemResponse(x.PublicId,x.Code,x.Name,x.Description,x.MinValue,x.MaxValue,x.IsRequired,x.IsActive,x.DisplayOrder,Convert.ToBase64String(x.RowVersion))).ToListAsync(token);
        var liabilities=await db.DisputeLiabilityTypes.AsNoTracking().OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Id).Select(x=>new TrustMasterItemResponse(x.PublicId,x.Code,x.Name,x.Description,x.IsActive,x.DisplayOrder)).ToListAsync(token);
        var sanctions=await db.SanctionTypes.AsNoTracking().OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Id).Select(x=>new TrustMasterItemResponse(x.PublicId,x.Code,x.Name,x.Description,x.IsActive,x.DisplayOrder)).ToListAsync(token);
        return new(ratings,liabilities,sanctions);
    }

    public async Task<TrustReferenceDataResponse> CreateRatingItemAsync(CreateTrustRatingItemRequest request,long actor,CancellationToken token)
    {
        Validate(request.Code,request.Name,request.MinValue,request.MaxValue,request.DisplayOrder);
        var code=request.Code.Trim().ToUpperInvariant(); if(await db.ReviewRatingItems.AnyAsync(x=>x.Code==code,token))throw Error("TRUST_RATING_CODE_DUPLICATE","이미 사용 중인 평가항목 코드입니다.",409);
        var now=DateTime.UtcNow;db.ReviewRatingItems.Add(new ReviewRatingItem{Code=code,Name=request.Name.Trim(),Description=request.Description?.Trim(),MinValue=request.MinValue,MaxValue=request.MaxValue,IsRequired=request.IsRequired,IsActive=request.IsActive,DisplayOrder=request.DisplayOrder,EffectiveFrom=now,CreatedAt=now,UpdatedAt=now,CreatedByUserId=actor,UpdatedByUserId=actor});await db.SaveChangesAsync(token);return await GetAsync(token);
    }

    public async Task<TrustReferenceDataResponse> UpdateRatingItemAsync(Guid id,UpdateTrustRatingItemRequest request,long actor,CancellationToken token)
    {
        Validate("EXISTING",request.Name,request.MinValue,request.MaxValue,request.DisplayOrder);var row=await db.ReviewRatingItems.SingleOrDefaultAsync(x=>x.PublicId==id,token)??throw Error("TRUST_RATING_ITEM_NOT_FOUND","평가항목을 찾을 수 없습니다.",404);
        try{if(!string.IsNullOrWhiteSpace(request.RowVersion))row.RowVersion=Convert.FromBase64String(request.RowVersion);}catch{throw Error("ROW_VERSION_INVALID","변경 버전 값이 올바르지 않습니다.");}
        row.Name=request.Name.Trim();row.Description=request.Description?.Trim();row.MinValue=request.MinValue;row.MaxValue=request.MaxValue;row.IsRequired=request.IsRequired;row.IsActive=request.IsActive;row.DisplayOrder=request.DisplayOrder;row.UpdatedAt=DateTime.UtcNow;row.UpdatedByUserId=actor;
        try{await db.SaveChangesAsync(token);}catch(DbUpdateConcurrencyException){throw Error("CONCURRENT_UPDATE","다른 관리자가 먼저 변경했습니다. 새로고침 후 다시 시도해 주세요.",409);}return await GetAsync(token);
    }

    private static void Validate(string code,string name,decimal min,decimal max,int order){if(string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(name)||min<0||max<=min||order<0)throw Error("TRUST_RATING_ITEM_INVALID","평가항목 이름, 점수 범위와 표시 순서를 확인해 주세요.");}
    private static TrustCalculationException Error(string code,string message,int status=400)=>new(code,message,status);
}
