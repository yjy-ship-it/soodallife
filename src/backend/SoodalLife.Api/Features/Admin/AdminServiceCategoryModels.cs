using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Admin;

public sealed record AdminCategorySummaryResponse(
    long MajorCount,
    long MiddleCount,
    long ServiceCount,
    long ActiveServiceCount);

public sealed record AdminCategoryOptionResponse(
    Guid Id,
    string Name,
    string? ExternalCode,
    string StatusCode,
    int SortOrder);

public sealed record AdminServiceCategoryListResponse(
    int TotalCount,
    IReadOnlyList<AdminServiceCategoryListItemResponse> Items);

public sealed record AdminServiceCategoryListItemResponse(
    Guid Id,
    string Name,
    string? ExternalCode,
    string StatusCode,
    int SortOrder,
    Guid MajorId,
    string MajorName,
    Guid MiddleId,
    string MiddleName);

public sealed record AdminServiceCategoryDetailResponse(
    Guid Id,
    string Name,
    string? ExternalCode,
    string StatusCode,
    int SortOrder,
    Guid MajorId,
    string MajorName,
    Guid MiddleId,
    string MiddleName,
    string? SearchKeywordsText,
    string? SearchSlug,
    string? SeoTitle,
    string? SeoDescription,
    bool IsSearchIndexable);

public sealed record UpdateAdminServiceCategoryRequest(
    [param: Required, StringLength(200, MinimumLength = 1)] string Name,
    [param: Required, StringLength(20)] string StatusCode,
    [param: Range(0, int.MaxValue)] int SortOrder,
    [param: StringLength(2000)] string? SearchKeywordsText,
    [param: StringLength(220)] string? SearchSlug,
    [param: StringLength(200)] string? SeoTitle,
    [param: StringLength(500)] string? SeoDescription,
    bool IsSearchIndexable = true);

public sealed class AdminServiceCategoryException(
    string businessCode,
    string message,
    int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
}
