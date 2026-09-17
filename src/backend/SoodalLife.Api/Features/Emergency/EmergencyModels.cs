namespace SoodalLife.Api.Features.Emergency;

public sealed record EmergencyAvailabilitySlotInput(byte DayOfWeek, TimeOnly? StartTime, TimeOnly? EndTime, bool Is24Hours);
public sealed record EmergencyServiceSettingInput(Guid ProviderServiceCategoryId, bool IsEnabled, IReadOnlyList<EmergencyAvailabilitySlotInput> Slots,
    decimal BaseDispatchFeeAmount = 0, string PaymentMode = "ON_SITE", decimal NoShowFeeAmount = 0,
    int NoShowWaitMinutes = 10, bool WorkFeeSeparate = true, string? AdditionalFeeText = null, string? PaymentInstruction = null);
public sealed record EmergencyExceptionInput(DateTime StartsAt, DateTime EndsAt, string? Reason);
public sealed record UpdateEmergencyAvailabilityInput(bool IsEnabled, DateTime? TemporarilyUnavailableUntil, string? TemporaryUnavailableReason,
    IReadOnlyList<EmergencyServiceSettingInput> Services, IReadOnlyList<EmergencyExceptionInput> Exceptions, string? RowVersion);
public sealed record EmergencyAvailabilitySlotResponse(Guid Id, byte DayOfWeek, TimeOnly? StartTime, TimeOnly? EndTime, bool Is24Hours);
public sealed record EmergencyServiceSettingResponse(Guid Id, Guid ProviderServiceCategoryId, Guid CategoryId, string CategoryName,
    bool IsApproved, bool EmergencyAllowedByCategory, bool IsEnabled, IReadOnlyList<EmergencyAvailabilitySlotResponse> Slots,
    decimal BaseDispatchFeeAmount, string PaymentMode, decimal NoShowFeeAmount, int NoShowWaitMinutes,
    bool WorkFeeSeparate, string? AdditionalFeeText, string? PaymentInstruction);
public sealed record EmergencyExceptionResponse(Guid Id, DateTime StartsAt, DateTime EndsAt, string? Reason);
public sealed record EmergencyPauseHistoryResponse(DateTime OccurredAt, string Action, DateTime? UnavailableUntil, string? Reason);
public sealed record EmergencyAvailabilityResponse(Guid Id, bool IsEnabled, DateTime? TemporarilyUnavailableUntil,
    string? TemporaryUnavailableReason, bool IsCurrentlyAvailable, string CurrentAvailabilityReason,
    IReadOnlyList<EmergencyServiceSettingResponse> Services, IReadOnlyList<EmergencyExceptionResponse> Exceptions,
    IReadOnlyList<EmergencyPauseHistoryResponse> PauseHistory, string RowVersion);

public sealed record EmergencyAvailabilityDecision(bool IsAvailable, string ReasonCode, long? ProviderServiceCategoryId = null);

public sealed record SubmitEmergencyResponseInput(bool IsAvailable, int? EtaMinutes, DateTime? EstimatedArrivalAt,
    string? Conditions, string IdempotencyKey, string RowVersion);
public sealed record EmergencyProviderRequestItem(Guid RequestId, string CategoryName, string AreaName, string Title,
    string? Description, DateTime RequestedAt, DateTime ResponseDeadlineAt, string DispatchStatus, string? ResponseStatus,
    bool PersonalInformationWithheld);
public sealed record EmergencyProviderAssignmentItem(Guid TransactionId, string CategoryName, string AreaName,
    string Title, string TransactionStatus, string EmergencyStatus, DateTime AssignedAt);
public sealed record EmergencyProviderResponseResult(Guid Id, string Status, int? EtaMinutes, DateTime? EstimatedArrivalAt,
    string? Conditions, decimal BaseDispatchFeeAmount, string PaymentMode, decimal NoShowFeeAmount,
    int NoShowWaitMinutes, bool WorkFeeSeparate, string? AdditionalFeeText, DateTime RespondedAt, string RowVersion);
public sealed record EmergencyCustomerResponseItem(Guid Id, Guid ProviderId, string ProviderName, string Status,
    int? EtaMinutes, DateTime? EstimatedArrivalAt, string? Conditions, decimal? TrustScore, string? TrustGrade,
    int PublicReviewCount, bool IsApproved, bool PersonalInformationWithheld, decimal BaseDispatchFeeAmount,
    string PaymentMode, decimal NoShowFeeAmount, int NoShowWaitMinutes, bool WorkFeeSeparate,
    string? AdditionalFeeText, string RowVersion);
public sealed record SelectEmergencyProviderInput(Guid ResponseId, string IdempotencyKey, string RowVersion, string DetailAddress, bool TermsAccepted = false);
public sealed record EmergencySelectionResult(Guid TransactionId, Guid ProviderId, Guid ChatRoomId, string Status,
    string PricingStatus, bool WalletCharged);
public sealed record EmergencyProgressInput(string EventType, string? Note, string IdempotencyKey);
public sealed record EmergencyProgressItem(Guid Id, string EventType, string? Note, DateTime OccurredAt);
public sealed record EmergencyAgreementResponse(decimal BaseDispatchFeeAmount, string PaymentMode, string PaymentStatus,
    decimal NoShowFeeAmount, int NoShowWaitMinutes, bool WorkFeeSeparate, string? AdditionalFeeText,
    string? PaymentInstruction, DateTime? NoShowWaitUntil, string? NoShowStatus);
public sealed record EmergencyProgressResponse(Guid TransactionId, string TransactionStatus, string CurrentEmergencyStatus,
    IReadOnlyList<EmergencyProgressItem> Events, EmergencyAgreementResponse Agreement);
public sealed record EmergencyPaymentReportInput(string IdempotencyKey);
public sealed record EmergencyPaymentDecisionInput(string Decision, string? Reason, string IdempotencyKey);
public sealed record EmergencyNoShowInput(string SubjectRole, int ContactAttempts, string? EvidenceNote, string IdempotencyKey);
public sealed record EmergencyNoShowDisputeInput(string Reason, string IdempotencyKey);

public sealed class EmergencyWorkflowException(string businessCode, string message, int statusCode = StatusCodes.Status409Conflict)
    : Exception(message)
{
    public string BusinessCode { get; } = businessCode;
    public int StatusCode { get; } = statusCode;
}
