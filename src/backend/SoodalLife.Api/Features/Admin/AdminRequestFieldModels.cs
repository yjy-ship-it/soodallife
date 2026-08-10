using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Admin;

public sealed record AdminRequestFieldSummaryResponse(
    long TotalDefinitions,
    long TotalAssignments,
    IReadOnlyList<AdminRequestFieldTypeCountResponse> InputTypes);

public sealed record AdminRequestFieldTypeCountResponse(string Code, long Count);

public sealed record AdminRequestFieldResponse(
    Guid Id,
    string Label,
    string InputType,
    bool Required,
    int DisplayOrder,
    string DefinitionStatus,
    bool ServiceEnabled,
    string AssignmentScope,
    IReadOnlyList<AdminRequestFieldOptionResponse> Options,
    string? Unit,
    string ValidationRule,
    bool HasOptions,
    bool HasValidation,
    int AffectedServiceCount);

public sealed record AdminRequestFieldOptionResponse(
    Guid Id,
    string Value,
    string Label,
    int DisplayOrder,
    bool IsActive);

public sealed record UpdateAdminRequestFieldDefinitionRequest(
    [param: Required, StringLength(200, MinimumLength = 1)] string Label,
    [param: Required, StringLength(20)] string InputType,
    [param: Required, StringLength(20)] string StatusCode,
    [param: StringLength(2000)] string? Unit,
    [param: StringLength(1000)] string? ValidationRule,
    bool ConfirmSharedChange);

public sealed record UpdateAdminRequestFieldAssignmentRequest(
    bool IsActive,
    bool IsRequired,
    [param: Range(0, int.MaxValue)] int DisplayOrder);

public sealed record UpdateAdminRequestFieldOptionRequest(
    [param: Required, StringLength(1000, MinimumLength = 1)] string Label,
    [param: Range(0, int.MaxValue)] int DisplayOrder,
    bool IsActive);
