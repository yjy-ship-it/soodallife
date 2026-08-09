namespace SoodalLife.Api.Features.Providers;

public sealed record ProviderProfileResponse(
    Guid Id,
    string BusinessName,
    string ApprovalStatus,
    string ActivityStatus);

public sealed record ProviderServiceCategoryResponse(
    Guid CategoryId,
    string CategoryPath,
    string Status);

public sealed record ReplaceProviderServiceCategoriesInput(IReadOnlyList<Guid> CategoryIds);

public sealed record ProviderServiceAreaResponse(
    Guid ServiceCategoryId,
    string CategoryPath,
    IReadOnlyList<ProviderAreaResponse> Areas);

public sealed record ProviderAreaResponse(Guid Id, string Name, string AreaCode);

public sealed record ProviderServiceAreaSelection(
    Guid ServiceCategoryId,
    IReadOnlyList<Guid> AdministrativeAreaIds);

public sealed record ReplaceProviderServiceAreasInput(
    IReadOnlyList<ProviderServiceAreaSelection> Services);

public sealed class ProviderConfigurationException(
    string businessCode,
    string message,
    int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
}
