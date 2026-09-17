using System.ComponentModel.DataAnnotations;

namespace SoodalLife.Api.Features.Admin;

public sealed record AdminProviderRequirementStandardsResponse(
    IReadOnlyList<AdminProviderRequirementTypeResponse> RequirementTypes,
    IReadOnlyList<AdminProviderRequirementDefinitionResponse> RequirementDefinitions,
    IReadOnlyList<AdminProviderDocumentTypeResponse> DocumentTypes);

public sealed record AdminProviderRequirementTypeResponse(string Code, string Name, bool IsActive);
public sealed record SaveAdminProviderRequirementTypeRequest(
    [param: Required, StringLength(30, MinimumLength = 1)] string Code,
    [param: Required, StringLength(100, MinimumLength = 1)] string Name,
    bool IsActive);

public sealed record AdminProviderRequirementDefinitionResponse(
    Guid Id,
    string RequirementTypeCode,
    string RequirementCode,
    string Name,
    string? Description,
    bool IsActive);
public sealed record SaveAdminProviderRequirementDefinitionRequest(
    [param: Required, StringLength(30, MinimumLength = 1)] string RequirementTypeCode,
    [param: Required, StringLength(50, MinimumLength = 1)] string RequirementCode,
    [param: Required, StringLength(200, MinimumLength = 1)] string Name,
    [param: StringLength(1000)] string? Description,
    bool IsActive);

public sealed record AdminProviderDocumentTypeResponse(Guid Id, string Code, string Name, bool SupportsExpiry, bool IsActive);
public sealed record SaveAdminProviderDocumentTypeRequest(
    [param: Required, StringLength(50, MinimumLength = 1)] string Code,
    [param: Required, StringLength(200, MinimumLength = 1)] string Name,
    bool SupportsExpiry,
    bool IsActive);

public sealed record AdminProviderRequirementDefaultsResponse(
    int CreatedRequirementTypeCount,
    int CreatedRequirementDefinitionCount,
    int CreatedDocumentTypeCount,
    int ExistingRequirementTypeCount,
    int ExistingRequirementDefinitionCount,
    int ExistingDocumentTypeCount);
