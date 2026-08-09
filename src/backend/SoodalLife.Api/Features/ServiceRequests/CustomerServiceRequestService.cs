using System.Security.Claims;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Domain.Entities;
using SoodalLife.Api.Features.Catalog;
using SoodalLife.Api.Features.Matching;
using SoodalLife.Api.Infrastructure.Persistence;

namespace SoodalLife.Api.Features.ServiceRequests;

public sealed class CustomerServiceRequestService(
    SoodalLifeDbContext dbContext,
    RequestMatchingService matchingService)
{
    public async Task<ServiceRequestCreatedResponse> CreateAsync(
        ClaimsPrincipal principal,
        CreateServiceRequestInput input,
        CancellationToken cancellationToken)
    {
        var identity = await GetCustomerIdentityAsync(principal, cancellationToken);
        ValidateBasicInput(input);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var category = await dbContext.ServiceCategories.SingleOrDefaultAsync(item =>
            item.PublicId == input.CategoryId && item.LevelCode == "SERVICE" && item.StatusCode == "ACTIVE", cancellationToken);
        if (category?.ParentId is null)
        {
            throw Validation("CATEGORY_NOT_ACTIVE", "선택한 서비스 카테고리를 사용할 수 없습니다.", "categoryId");
        }

        var policy = await dbContext.CategoryPolicies
            .Where(item => item.CategoryId == category.Id && item.TransactionTypeCode == "ONE_TIME" &&
                           item.EffectiveFrom <= today && (item.EffectiveTo == null || item.EffectiveTo > today))
            .OrderByDescending(item => item.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (policy is null)
        {
            throw Validation("CATEGORY_NOT_ACTIVE", "현재 적용 가능한 카테고리 정책이 없습니다.", "categoryId");
        }

        var area = await dbContext.AdministrativeAreas.SingleOrDefaultAsync(item =>
            item.PublicId == input.AdministrativeAreaId && item.AreaLevelCode == "SIGUNGU" && item.IsActive, cancellationToken);
        if (area is null)
        {
            throw Validation("SERVICE_AREA_INVALID", "사용 가능한 시·군·구를 선택해 주세요.", "administrativeAreaId");
        }

        var fields = await (
                from assignment in dbContext.CategoryFieldAssignments
                join field in dbContext.CategoryFieldDefinitions on assignment.FieldDefinitionId equals field.Id
                where assignment.IsActive && field.StatusCode == "ACTIVE" &&
                      (assignment.TargetCategoryId == category.Id || assignment.TargetCategoryId == category.ParentId)
                select field)
            .ToListAsync(cancellationToken);
        var answersByFieldId = input.Answers
            .GroupBy(answer => answer.FieldId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        if (answersByFieldId.Any(pair => pair.Value.Length > 1))
        {
            throw Validation("DYNAMIC_FIELD_DUPLICATE", "같은 동적 필드에 여러 답변을 보낼 수 없습니다.", "answers");
        }

        var applicablePublicIds = fields.Select(field => field.PublicId).ToHashSet();
        if (answersByFieldId.Keys.Any(id => !applicablePublicIds.Contains(id)))
        {
            throw Validation("DYNAMIC_FIELD_INVALID", "선택한 카테고리에 적용되지 않는 필드가 포함되어 있습니다.", "answers");
        }

        var normalizedAnswers = new List<RequestAnswer>();
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            var provided = answersByFieldId.GetValueOrDefault(field.PublicId)?.SingleOrDefault();
            if (provided is null || IsEmpty(provided.Value))
            {
                if (field.IsRequired)
                {
                    errors[$"answers.{field.FieldKey}"] = ["필수 입력 항목입니다."];
                }

                continue;
            }

            try
            {
                normalizedAnswers.Add(NormalizeAnswer(field, provided.Value, identity.UserId));
            }
            catch (FormatException exception)
            {
                errors[$"answers.{field.FieldKey}"] = [exception.Message];
            }
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException("DYNAMIC_FIELD_REQUIRED", "동적 입력필드를 확인해 주세요.", errors);
        }

        var now = DateTime.UtcNow;
        var request = new ServiceRequest
        {
            CustomerProfileId = identity.CustomerProfileId,
            CategoryId = category.Id,
            CategoryPolicyId = policy.Id,
            AdministrativeAreaId = area.Id,
            DetailAddress = NullIfEmpty(input.DetailAddress),
            Title = input.Title.Trim(),
            Description = NullIfEmpty(input.Description),
            StatusCode = "DRAFT",
            IsUrgent = input.IsUrgent,
            IdempotencyKey = NullIfEmpty(input.IdempotencyKey),
            PolicySnapshotJson = JsonSerializer.Serialize(new
            {
                categoryId = category.PublicId,
                categoryCode = category.ExternalCode,
                categoryName = category.Name,
                policyId = policy.PublicId,
                policyVersion = policy.PolicyVersion,
                policy.MaxQuoteCount,
                policy.QuoteValidityMinutes,
                policy.ProviderResponseDeadlineMinutes,
                policy.RequiredCompletionPhotoCount,
            }),
            CreatedAt = now,
            CreatedByUserId = identity.UserId,
            UpdatedAt = now,
            UpdatedByUserId = identity.UserId,
        };

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        dbContext.ServiceRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var answer in normalizedAnswers)
        {
            answer.ServiceRequestId = request.Id;
            answer.CreatedAt = now;
            answer.UpdatedAt = now;
            dbContext.RequestAnswers.Add(answer);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new ServiceRequestCreatedResponse(request.PublicId, request.StatusCode);
    }

    public async Task<IReadOnlyList<ServiceRequestListItemResponse>> GetMineAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var identity = await GetCustomerIdentityAsync(principal, cancellationToken);
        var rows = await (
                from request in dbContext.ServiceRequests.AsNoTracking()
                join service in dbContext.ServiceCategories.AsNoTracking() on request.CategoryId equals service.Id
                join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                where request.CustomerProfileId == identity.CustomerProfileId
                orderby request.CreatedAt descending
                select new
                {
                    request.Id,
                    request.PublicId,
                    request.Title,
                    request.StatusCode,
                    request.CreatedAt,
                    Path = major.Name + " > " + middle.Name + " > " + service.Name,
                })
            .ToListAsync(cancellationToken);
        var requestIds = rows.Select(row => row.Id).ToArray();
        var desiredDates = await (
                from answer in dbContext.RequestAnswers.AsNoTracking()
                join field in dbContext.CategoryFieldDefinitions.AsNoTracking() on answer.FieldDefinitionId equals field.Id
                where requestIds.Contains(answer.ServiceRequestId) && field.FieldKey == "desired_date"
                select new { answer.ServiceRequestId, answer.ValueDateTime })
            .ToDictionaryAsync(item => item.ServiceRequestId, item => item.ValueDateTime, cancellationToken);

        return rows.Select(row => new ServiceRequestListItemResponse(
            row.PublicId,
            row.Title,
            row.StatusCode,
            row.Path,
            row.CreatedAt,
            desiredDates.GetValueOrDefault(row.Id))).ToArray();
    }

    public async Task<PublishServiceRequestResponse> PublishAsync(
        ClaimsPrincipal principal,
        Guid requestPublicId,
        CancellationToken cancellationToken)
    {
        var identity = await GetCustomerIdentityAsync(principal, cancellationToken);
        var request = await dbContext.ServiceRequests.SingleOrDefaultAsync(item =>
            item.PublicId == requestPublicId && item.CustomerProfileId == identity.CustomerProfileId,
            cancellationToken);
        if (request is null)
        {
            throw Validation("REQUEST_NOT_FOUND", "서비스 요청을 찾을 수 없습니다.", "requestId");
        }
        if (request.StatusCode is not ("DRAFT" or "OPEN"))
        {
            throw Validation("REQUEST_NOT_PUBLISHABLE", "임시저장 또는 공개 상태의 요청만 공개할 수 있습니다.", "requestId");
        }

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (request.StatusCode == "DRAFT")
        {
            var policy = await dbContext.CategoryPolicies.SingleAsync(item => item.Id == request.CategoryPolicyId, cancellationToken);
            var now = DateTime.UtcNow;
            request.StatusCode = "OPEN";
            request.OpenedAt = now;
            request.ExpiresAt = now.AddMinutes(policy.QuoteValidityMinutes);
            request.UpdatedAt = now;
            request.UpdatedByUserId = identity.UserId;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var matching = await matchingService.MatchAndDispatchAsync(request, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new PublishServiceRequestResponse(
            request.PublicId,
            request.StatusCode,
            matching.EligibleCandidateCount,
            matching.DispatchCount);
    }

    public async Task<ServiceRequestDetailResponse?> GetMineByIdAsync(
        ClaimsPrincipal principal,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var identity = await GetCustomerIdentityAsync(principal, cancellationToken);
        var row = await (
                from request in dbContext.ServiceRequests.AsNoTracking()
                join service in dbContext.ServiceCategories.AsNoTracking() on request.CategoryId equals service.Id
                join middle in dbContext.ServiceCategories.AsNoTracking() on service.ParentId equals middle.Id
                join major in dbContext.ServiceCategories.AsNoTracking() on middle.ParentId equals major.Id
                join area in dbContext.AdministrativeAreas.AsNoTracking() on request.AdministrativeAreaId equals area.Id
                where request.PublicId == publicId && request.CustomerProfileId == identity.CustomerProfileId
                select new
                {
                    Request = request,
                    Path = major.Name + " > " + middle.Name + " > " + service.Name,
                    AreaPublicId = area.PublicId,
                    AreaName = area.AreaName,
                })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var answers = await (
                from answer in dbContext.RequestAnswers.AsNoTracking()
                join field in dbContext.CategoryFieldDefinitions.AsNoTracking() on answer.FieldDefinitionId equals field.Id
                where answer.ServiceRequestId == row.Request.Id
                orderby field.DisplayOrder
                select new { Answer = answer, Field = field })
            .ToListAsync(cancellationToken);
        var answerResponses = answers.Select(item => new ServiceRequestAnswerResponse(
            item.Field.PublicId,
            item.Field.FieldKey,
            item.Field.Label,
            item.Field.FieldTypeCode,
            ReadAnswerValue(item.Answer))).ToArray();
        var desiredAt = answers.FirstOrDefault(item => item.Field.FieldKey == "desired_date")?.Answer.ValueDateTime;

        return new ServiceRequestDetailResponse(
            row.Request.PublicId,
            row.Request.Title,
            row.Request.Description,
            row.Request.StatusCode,
            row.Request.IsUrgent,
            row.Path,
            row.AreaPublicId,
            row.AreaName,
            row.Request.DetailAddress,
            row.Request.CreatedAt,
            desiredAt,
            answerResponses);
    }

    private async Task<CustomerIdentity> GetCustomerIdentityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId))
        {
            throw new InvalidOperationException("Authenticated user identifier is invalid.");
        }

        var identity = await (
                from user in dbContext.Users.AsNoTracking()
                join customer in dbContext.CustomerProfiles.AsNoTracking() on user.Id equals customer.UserId
                where user.PublicId == publicId && user.StatusCode == "ACTIVE"
                select new CustomerIdentity(user.Id, customer.Id))
            .SingleOrDefaultAsync(cancellationToken);
        return identity ?? throw new InvalidOperationException("The authenticated CUSTOMER role has no customer profile.");
    }

    private static RequestAnswer NormalizeAnswer(CategoryFieldDefinition field, JsonElement value, long userId)
    {
        var answer = new RequestAnswer { FieldDefinitionId = field.Id, CreatedByUserId = userId, UpdatedByUserId = userId };
        switch (field.FieldTypeCode)
        {
            case "TEXT":
            case "LONG_TEXT":
            case "TEXTAREA":
            case "ADDRESS":
                answer.ValueText = RequireString(value);
                if (field.ValidationRuleText.Contains("20자 이상", StringComparison.Ordinal) && answer.ValueText.Length < 20)
                {
                    throw new FormatException("20자 이상 입력해 주세요.");
                }
                break;
            case "SELECT":
            case "RADIO":
                answer.ValueText = RequireString(value);
                var options = CatalogQueryService.ParseOptions(field.FieldTypeCode, field.OptionsOrUnitText);
                if (options.Count > 0 && !options.Contains(answer.ValueText, StringComparer.Ordinal))
                {
                    throw new FormatException("허용된 선택값을 입력해 주세요.");
                }
                break;
            case "NUMBER":
            case "MONEY":
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number))
                {
                    throw new FormatException("숫자 형식으로 입력해 주세요.");
                }
                if (field.ValidationRuleText.Contains("0 이상", StringComparison.Ordinal) && number < 0)
                {
                    throw new FormatException("0 이상의 값을 입력해 주세요.");
                }
                answer.ValueNumber = number;
                answer.ValueCurrencyCode = field.FieldTypeCode == "MONEY" ? "KRW" : null;
                break;
            case "BOOLEAN":
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    throw new FormatException("참 또는 거짓 값을 입력해 주세요.");
                }
                answer.ValueBoolean = value.GetBoolean();
                break;
            case "DATE":
                if (!DateOnly.TryParse(RequireString(value), out var date))
                {
                    throw new FormatException("날짜 형식으로 입력해 주세요.");
                }
                answer.ValueDate = date;
                break;
            case "DATETIME":
                if (!DateTimeOffset.TryParse(RequireString(value), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
                {
                    throw new FormatException("일시 형식으로 입력해 주세요.");
                }
                answer.ValueDateTime = dateTime.UtcDateTime;
                if (field.ValidationRuleText.Contains("현재 이후", StringComparison.Ordinal) && answer.ValueDateTime <= DateTime.UtcNow)
                {
                    throw new FormatException("현재 이후의 일시를 입력해 주세요.");
                }
                break;
            case "FILE":
                ValidateFileMetadata(value);
                answer.ValueJson = value.GetRawText();
                break;
            case "CHECKBOX":
                if (value.ValueKind != JsonValueKind.Array)
                {
                    throw new FormatException("복수 선택값 배열을 입력해 주세요.");
                }
                answer.ValueJson = value.GetRawText();
                break;
            case "PERIOD":
            case "RECURRENCE":
                answer.ValueJson = value.GetRawText();
                break;
            default:
                throw new FormatException($"지원하지 않는 입력유형입니다: {field.FieldTypeCode}");
        }

        return answer;
    }

    private static object? ReadAnswerValue(RequestAnswer answer)
    {
        if (answer.ValueText is not null) return answer.ValueText;
        if (answer.ValueNumber is not null) return answer.ValueNumber;
        if (answer.ValueBoolean is not null) return answer.ValueBoolean;
        if (answer.ValueDate is not null) return answer.ValueDate.Value.ToString("yyyy-MM-dd");
        if (answer.ValueDateTime is not null) return answer.ValueDateTime.Value;
        return answer.ValueJson is null ? null : JsonSerializer.Deserialize<JsonElement>(answer.ValueJson);
    }

    private static void ValidateBasicInput(CreateServiceRequestInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 200)
        {
            throw Validation("REQUEST_INVALID", "요청 제목을 1~200자로 입력해 주세요.", "title");
        }
        if (input.DetailAddress?.Length > 500)
        {
            throw Validation("REQUEST_INVALID", "상세주소는 500자 이하여야 합니다.", "detailAddress");
        }
        if (input.IdempotencyKey?.Length > 100)
        {
            throw Validation("REQUEST_INVALID", "중복방지 키가 너무 깁니다.", "idempotencyKey");
        }
        if (input.Answers is null)
        {
            throw Validation("REQUEST_INVALID", "동적 답변 목록이 필요합니다.", "answers");
        }
    }

    private static void ValidateFileMetadata(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
        {
            throw new FormatException("한 개 이상의 파일 메타데이터가 필요합니다.");
        }
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(nameElement.GetString()) || nameElement.GetString()!.Length > 255 ||
                Path.GetFileName(nameElement.GetString()) != nameElement.GetString() ||
                !item.TryGetProperty("sizeBytes", out var sizeElement) || !sizeElement.TryGetInt64(out var size) || size < 0)
            {
                throw new FormatException("파일명과 크기를 포함한 안전한 파일 메타데이터를 입력해 주세요.");
            }
        }
    }

    private static bool IsEmpty(JsonElement value) => value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ||
        (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString())) ||
        (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0);
    private static string RequireString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : throw new FormatException("문자열 값을 입력해 주세요.");
    private static RequestValidationException Validation(string code, string message, string field) =>
        new(code, message, new Dictionary<string, string[]> { [field] = [message] });
    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private sealed record CustomerIdentity(long UserId, long CustomerProfileId);
}
