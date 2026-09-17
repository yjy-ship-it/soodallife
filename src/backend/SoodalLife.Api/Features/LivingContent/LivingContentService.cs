using System.Security.Claims;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SoodalLife.Api.Infrastructure.Persistence;
using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Features.LivingContent;

public sealed class LivingContentService(SoodalLifeDbContext db,IMemoryCache cache,ILogger<LivingContentService> logger)
{
    private const string PrivacyNotice="공개 동의·공개 상태가 확인된 후기와 개인 식별정보를 제거한 지역 통계만 사용합니다. 상세주소와 개별 거래금액은 공개하지 않습니다.";

    public async Task<LivingHomeResponse> GetAsync(ClaimsPrincipal principal,CancellationToken token)
    {
        var stopwatch=Stopwatch.StartNew();var now=DateTime.UtcNow;var localNow=now.AddHours(9);var customerId=await CustomerId(principal,token);
        var area=customerId.HasValue?await DefaultArea(customerId.Value,token):null;
        var checks=DailyChecks(localNow);
        var insightsKey=$"living-home:v172:public:insights:{area?.Id??0}";var insightsHit=cache.TryGetValue<IReadOnlyList<LocalServiceInsight>>(insightsKey,out var insights);
        if(!insightsHit||insights is null){insights=await LocalInsights(area?.Id,area?.Name??"전국",now,token);cache.Set(insightsKey,insights,TimeSpan.FromMinutes(5));}
        const string storiesKey="living-home:v172:public:stories";var storiesHit=cache.TryGetValue<IReadOnlyList<PublicWorkStory>>(storiesKey,out var stories);
        if(!storiesHit||stories is null){stories=await WorkStories(now,token);cache.Set(storiesKey,stories,TimeSpan.FromMinutes(5));}
        const string answersKey="living-home:v172:public:answers";var answersHit=cache.TryGetValue<IReadOnlyList<ExpertQuickAnswer>>(answersKey,out var answers);
        if(!answersHit||answers is null){answers=await ExpertAnswers(token);cache.Set(answersKey,answers,TimeSpan.FromMinutes(5));}
        IReadOnlyList<MaintenanceCalendarItem> calendar;var customerCacheHit=false;
        if(customerId.HasValue){var calendarKey=$"living-home:v172:customer:{customerId.Value}:calendar";customerCacheHit=cache.TryGetValue<IReadOnlyList<MaintenanceCalendarItem>>(calendarKey,out var cachedCalendar);if(!customerCacheHit||cachedCalendar is null){cachedCalendar=await Maintenance(customerId,localNow,token);cache.Set(calendarKey,cachedCalendar,TimeSpan.FromMinutes(1));}calendar=cachedCalendar;}
        else calendar=await Maintenance(null,localNow,token);
        stopwatch.Stop();logger.LogInformation("Living home completed. Personalized={Personalized} PublicCacheHit={PublicCacheHit} CustomerCacheHit={CustomerCacheHit} DurationMs={DurationMs}",customerId.HasValue,insightsHit&&storiesHit&&answersHit,customerCacheHit,stopwatch.Elapsed.TotalMilliseconds);
        return new(checks,insights,calendar,stories,answers,area?.Name??"전국",customerId.HasValue,now,PrivacyNotice);
    }

    private async Task<long?> CustomerId(ClaimsPrincipal principal,CancellationToken token)
    {
        if(!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier),out var publicId))return null;
        return await(from user in db.Users.AsNoTracking() join customer in db.CustomerProfiles.AsNoTracking() on user.Id equals customer.UserId where user.PublicId==publicId&&user.StatusCode=="ACTIVE" select(long?)customer.Id).SingleOrDefaultAsync(token);
    }

    private async Task<AreaInfo?> DefaultArea(long customerId,CancellationToken token)
    {
        var row=await db.CustomerAddresses.AsNoTracking().Where(x=>x.CustomerProfileId==customerId&&x.IsActive&&x.AdministrativeAreaId!=null).OrderByDescending(x=>x.IsDefault).ThenByDescending(x=>x.UpdatedAt).Select(x=>x.AdministrativeAreaId).FirstOrDefaultAsync(token);
        if(!row.HasValue)return null;var area=await db.AdministrativeAreas.AsNoTracking().Where(x=>x.Id==row.Value).Select(x=>new{x.Id,x.AreaName,x.ParentAreaId}).SingleOrDefaultAsync(token);if(area is null)return null;
        var parent=area.ParentAreaId.HasValue?await db.AdministrativeAreas.AsNoTracking().Where(x=>x.Id==area.ParentAreaId).Select(x=>x.AreaName).SingleOrDefaultAsync(token):null;
        return new(area.Id,string.IsNullOrWhiteSpace(parent)||parent==area.AreaName?area.AreaName:$"{parent} {area.AreaName}");
    }

    private async Task<IReadOnlyList<LocalServiceInsight>> LocalInsights(long? areaId,string regionName,DateTime now,CancellationToken token)
    {
        var cutoff=now.AddDays(-90);
        var rows=await(from transaction in db.Transactions.AsNoTracking()
                       join request in db.ServiceRequests.AsNoTracking() on transaction.ServiceRequestId equals request.Id
                       join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
                       where transaction.StatusCode=="COMPLETED"&&transaction.CompletedAt>=cutoff&&transaction.AgreedAmount>0&&(!areaId.HasValue||request.AdministrativeAreaId==areaId)
                       select new{category.Name,category.SearchSlug,transaction.AgreedAmount,transaction.CurrencyCode}).Take(1000).ToListAsync(token);
        if(areaId.HasValue&&rows.Count<3){regionName="전국";rows=await(from transaction in db.Transactions.AsNoTracking() join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id where transaction.StatusCode=="COMPLETED"&&transaction.CompletedAt>=cutoff&&transaction.AgreedAmount>0 select new{category.Name,category.SearchSlug,transaction.AgreedAmount,transaction.CurrencyCode}).Take(1000).ToListAsync(token);}
        return rows.GroupBy(x=>new{x.Name,x.SearchSlug,x.CurrencyCode}).Where(x=>x.Count()>=3).OrderByDescending(x=>x.Count()).ThenBy(x=>x.Key.Name).Take(4).Select(group=>
        {
            var values=group.Select(x=>x.AgreedAmount).Order().ToArray();var low=values[(int)Math.Floor((values.Length-1)*.25)];var high=values[(int)Math.Ceiling((values.Length-1)*.75)];
            return new LocalServiceInsight(group.Key.Name,regionName,group.Count(),Round(low,group.Key.CurrencyCode),Round(high,group.Key.CurrencyCode),group.Key.CurrencyCode,90,Path(group.Key.SearchSlug,group.Key.Name),"최근 90일 완료 거래의 중간 50% 범위");
        }).ToArray();
    }

    private async Task<IReadOnlyList<MaintenanceCalendarItem>> Maintenance(long? customerId,DateTime localNow,CancellationToken token)
    {
        var result=new List<MaintenanceCalendarItem>();
        if(customerId.HasValue)
        {
            var history=await(from entry in db.ServiceHistoryEntries.AsNoTracking() join transaction in db.Transactions.AsNoTracking() on entry.TransactionId equals transaction.Id join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id where entry.CustomerProfileId==customerId&&entry.EventTypeCode=="COMPLETION" select new{entry.PublicId,entry.CompletedAtSnapshot,entry.OccurredAt,category.Name,category.SearchSlug}).ToListAsync(token);
            foreach(var row in history.GroupBy(x=>new{x.Name,x.SearchSlug}).Select(x=>x.OrderByDescending(v=>v.CompletedAtSnapshot??v.OccurredAt).First()).OrderByDescending(x=>x.CompletedAtSnapshot??x.OccurredAt).Take(4))
            {
                var last=row.CompletedAtSnapshot??row.OccurredAt;var interval=IntervalDays(row.Name);var due=last.AddDays(interval);result.Add(new($"history-{row.PublicId:N}",$"{row.Name} 관리 시기",$"마지막 이용 {last.AddHours(9):yyyy. M. d.} · 권장 주기 {interval}일",DateOnly.FromDateTime(due.AddHours(9)),Status(due,localNow),"내 실제 서비스 이력",Path(row.SearchSlug,row.Name)));
            }
            var assets=await db.ServiceAssets.AsNoTracking().Where(x=>x.CustomerProfileId==customerId&&x.StatusCode=="ACTIVE"&&x.InstalledAt!=null).OrderBy(x=>x.InstalledAt).Take(3).Select(x=>new{x.PublicId,x.Name,x.InstalledAt}).ToListAsync(token);
            foreach(var asset in assets){var due=asset.InstalledAt!.Value.AddYears(1);while(due<DateOnly.FromDateTime(localNow))due=due.AddYears(1);result.Add(new($"asset-{asset.PublicId:N}",$"{asset.Name} 정기 점검","등록된 생활설비의 연간 점검일입니다.",due,due<=DateOnly.FromDateTime(localNow.AddDays(30))?"DUE_SOON":"UPCOMING","내 생활설비",$"/services/search?q={Uri.EscapeDataString(asset.Name)}"));}
        }
        foreach(var item in SeasonalCalendar(localNow))if(result.All(x=>x.Title!=item.Title))result.Add(item);
        return result.OrderBy(x=>x.DueDate).Take(6).ToArray();
    }

    private async Task<IReadOnlyList<PublicWorkStory>> WorkStories(DateTime now,CancellationToken token)
    {
        var cutoff=now.AddDays(-180);
        var rows=await(from review in db.Reviews.AsNoTracking()
                       join transaction in db.Transactions.AsNoTracking() on review.TransactionId equals transaction.Id
                       join request in db.ServiceRequests.AsNoTracking() on transaction.ServiceRequestId equals request.Id
                       join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
                       join provider in db.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
                       join area in db.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id into areaRows from area in areaRows.DefaultIfEmpty()
                       where review.VisibilityStatusCode=="PUBLIC"&&review.VerificationStatusCode=="VERIFIED_TRANSACTION"&&review.PublishedAt>=cutoff&&transaction.StatusCode=="COMPLETED"
                       orderby review.PublishedAt descending
                       select new{review.PublicId,review.BodyText,review.OverallRating,transaction.CompletedAt,category.Name,category.SearchSlug,provider.BusinessName,AreaName=area==null?"전국":area.AreaName}).Take(8).ToListAsync(token);
        return rows.Select(x=>new PublicWorkStory(x.PublicId,x.Name,x.AreaName,x.BusinessName,Safe(x.BodyText,150),x.OverallRating,x.CompletedAt??now,Path(x.SearchSlug,x.Name))).ToArray();
    }

    private async Task<IReadOnlyList<ExpertQuickAnswer>> ExpertAnswers(CancellationToken token)
    {
        var rows=await(from answer in db.ReviewComments.AsNoTracking()
                       join question in db.ReviewComments.AsNoTracking() on answer.ParentCommentId equals question.Id
                       join review in db.Reviews.AsNoTracking() on answer.ReviewId equals review.Id
                       join transaction in db.Transactions.AsNoTracking() on review.TransactionId equals transaction.Id
                       join category in db.ServiceCategories.AsNoTracking() on transaction.CategoryId equals category.Id
                       join provider in db.ProviderProfiles.AsNoTracking() on transaction.ProviderProfileId equals provider.Id
                       where answer.StatusCode=="ACTIVE"&&question.StatusCode=="ACTIVE"&&answer.AuthorRoleCode=="PROVIDER"&&review.VisibilityStatusCode=="PUBLIC"&&review.VerificationStatusCode=="VERIFIED_TRANSACTION"
                       orderby answer.SubmittedAt descending
                       select new{answer.PublicId,Question=question.BodyText,Answer=answer.BodyText,provider.BusinessName,category.Name,category.SearchSlug,answer.SubmittedAt}).Take(6).ToListAsync(token);
        return rows.Select(x=>new ExpertQuickAnswer(x.PublicId,Safe(x.Question,100),Safe(x.Answer,180),x.BusinessName,x.Name,x.SubmittedAt,Path(x.SearchSlug,x.Name))).ToArray();
    }

    private static IReadOnlyList<DailyLivingCheck> DailyChecks(DateTime now)
    {
        var month=now.Month;var season=month is 12 or 1 or 2?"겨울 점검":month is 3 or 4 or 5?"봄 점검":month is 6 or 7 or 8?"여름 점검":"가을 점검";
        var values=month switch
        {
            12 or 1 or 2=>new[]{("freeze","수도·보일러 동파 징후가 없나요?","외부 수도와 보일러 배관 보온 상태를 3분만 확인해 보세요.","동파"),("lock","현관문 잠금장치가 뻑뻑하지 않나요?","추운 날에는 배터리 성능과 잠금쇠 정렬 상태를 함께 확인하세요.","도어락"),("electric","전열기 사용 전 콘센트를 확인하세요","변색·탄 냄새·헐거움이 있으면 사용을 멈추고 점검이 필요합니다.","전기 점검"),("boiler-pressure","보일러 압력과 에러 표시를 확인하세요","압력이 반복해서 떨어지거나 오류가 보이면 무리하게 재가동하지 마세요.","보일러 점검"),("window-water","창문 결로가 반복되나요?","실내 환기와 창틀 배수 상태를 확인해 곰팡이와 누수를 예방하세요.","창호"),("heater-dust","난방기 주변에 먼지가 쌓이지 않았나요?","흡입구와 전선 주변 먼지를 정리하고 과열 흔적을 확인하세요.","난방기 청소")},
            3 or 4 or 5=>new[]{("air","에어컨을 켜기 전 필터를 확인하세요","필터 먼지와 실외기 주변 장애물을 미리 정리하면 고장을 줄일 수 있습니다.","에어컨 청소"),("screen","방충망과 창호 틈새를 확인하세요","찢어진 망과 창틀 틈을 미리 보수하면 벌레와 빗물 유입을 줄일 수 있습니다.","방충망"),("bath","욕실 실리콘에 검은 점이 생겼나요?","곰팡이가 반복되거나 들뜬 부분은 재시공 시기를 확인해 보세요.","욕실 실리콘"),("drain-spring","배수구 냄새가 올라오지 않나요?","트랩의 물과 배수 속도를 확인하면 악취와 막힘을 일찍 발견할 수 있습니다.","배수구"),("outlet-spring","겨울 동안 사용한 멀티탭을 확인하세요","변색되거나 헐거운 멀티탭은 교체하고 문어발 연결을 정리하세요.","전기 점검"),("clean-window","창틀과 유리의 묵은 먼지를 확인하세요","미세먼지가 많은 시기에는 창틀 홈과 방충망을 함께 청소하세요.","창문 청소")},
            6 or 7 or 8=>new[]{("drain","장마 전 창틀 배수구를 확인하세요","먼지로 막힌 배수구는 실내 누수의 원인이 될 수 있습니다.","창호"),("air","에어컨 냄새와 물 떨어짐을 확인하세요","필터 청소 후에도 냄새나 누수가 계속되면 전문가 점검이 필요합니다.","에어컨"),("leak","천장·벽지에 번진 자국이 없나요?","젖은 자국이 커지면 사진을 남기고 누수 위치를 조기에 확인하세요.","누수"),("bath-fan","욕실 환풍기 흡입력이 약하지 않나요?","습기가 오래 남으면 필터와 배기구를 확인해 곰팡이를 예방하세요.","환풍기"),("fridge","냉장고 뒤쪽이 지나치게 뜨겁지 않나요?","통풍 공간과 먼지를 확인하고 벽과 적정 거리를 유지하세요.","가전 청소"),("door-summer","현관문과 창문의 잠금 상태를 확인하세요","휴가 전 잠금장치와 방범창의 흔들림을 미리 살펴보세요.","도어락")},
            _=>new[]{("boiler","난방 전 보일러를 시험 가동하세요","소음·누수·에러코드를 미리 확인하면 갑작스러운 고장을 줄일 수 있습니다.","보일러"),("seal","창문 틈바람을 확인하세요","창호 고무와 실리콘 들뜸을 확인하면 난방 손실을 줄일 수 있습니다.","창호"),("hood","주방 후드 기름때를 확인하세요","흡입력이 약해졌다면 필터 청소 또는 교체 시기일 수 있습니다.","후드 청소"),("drain-fall","낙엽철 외부 배수구를 확인하세요","배수구 주변 이물질을 치우면 갑작스러운 비의 역류를 줄일 수 있습니다.","배수구"),("lighting","해가 짧아지기 전 조명을 확인하세요","깜빡임이 있거나 어두워진 조명은 배선과 함께 점검해 보세요.","조명"),("storage","겨울용품 보관 공간에 습기가 없나요?","붙박이장과 창고를 환기하고 곰팡이 흔적을 확인하세요.","곰팡이 제거")}
        };
        var offset=now.DayOfYear%values.Length;
        return Enumerable.Range(0,3).Select(index=>values[(offset+index)%values.Length]).Select(x=>new DailyLivingCheck(x.Item1,season,x.Item2,x.Item3,"점검 방법·서비스 보기",$"/services/search?q={Uri.EscapeDataString(x.Item4)}")).ToArray();
    }

    private static IEnumerable<MaintenanceCalendarItem> SeasonalCalendar(DateTime now)
    {
        var date=DateOnly.FromDateTime(now);yield return new("season-1",now.Month is >=6 and <=8?"냉방·배수 점검":"난방·배관 점검","계절이 바뀌기 전 주요 설비를 확인하세요.",date.AddDays(7),"DUE_SOON","계절 관리 추천",$"/services/search?q={Uri.EscapeDataString(now.Month is >=6 and <=8?"에어컨":"보일러")}");yield return new("season-2","주방·욕실 위생 점검","후드·환풍기·배수구 상태를 정기적으로 확인하세요.",date.AddDays(21),"UPCOMING","생활 관리 추천",$"/services/search?q={Uri.EscapeDataString("청소")}");
    }

    private static int IntervalDays(string name)=>name.Contains("에어컨")?180:name.Contains("청소")||name.Contains("후드")?90:name.Contains("보일러")||name.Contains("전기")||name.Contains("점검")?365:180;
    private static string Status(DateTime due,DateTime now)=>due<now?"OVERDUE":due<=now.AddDays(30)?"DUE_SOON":"UPCOMING";
    private static decimal Round(decimal value,string currency)=>currency=="KRW"?Math.Round(value/1000,MidpointRounding.AwayFromZero)*1000:Math.Round(value,2);
    private static string Path(string? slug,string name)=>string.IsNullOrWhiteSpace(slug)?$"/services/search?q={Uri.EscapeDataString(name)}":$"/services/{slug}";
    private static string Safe(string value,int max){var safe=SecurityTextSanitizer.Text(value)?.Replace('\r',' ').Replace('\n',' ').Trim()??string.Empty;return safe.Length<=max?safe:$"{safe[..max]}…";}
    private sealed record AreaInfo(long Id,string Name);
}
