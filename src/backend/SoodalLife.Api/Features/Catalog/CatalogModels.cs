namespace SoodalLife.Api.Features.Catalog;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Level,
    string? ExternalCode,
    int SortOrder);

public sealed record RequestFieldResponse(
    Guid Id,
    string FieldKey,
    string Label,
    string InputType,
    bool Required,
    string? Placeholder,
    IReadOnlyList<string> Options,
    string? HelperText,
    string ValidationRule,
    int SortOrder);

public sealed record AdministrativeAreaResponse(
    Guid Id,
    string Name,
    string AreaCode,
    Guid? ParentId,
    string? ParentName);
