using GLMS.Shared.Enums;

namespace GLMS.Shared.DTOs;

public sealed record ContractListItemDto(
    int ContractId,
    int ClientId,
    string ClientName,
    string Title,
    string ServiceLevel,
    DateTime StartDate,
    DateTime EndDate,
    ContractStatus Status,
    int ServiceRequestCount,
    string? PdfFileName,
    string? PdfOriginalFileName);

public sealed record ContractCreateCommand(
    int ClientId,
    string Title,
    string ServiceLevel,
    DateTime StartDate,
    DateTime EndDate);

public sealed record ContractTransitionCommand(ContractTransitionAction Action);

public sealed record ContractTransitionResultDto(
    bool Succeeded,
    int ContractId,
    ContractStatus PreviousStatus,
    ContractStatus CurrentStatus,
    string Message);

public sealed record ContractAgreementUploadResultDto(
    bool Succeeded,
    int ContractId,
    string Message,
    string? StoredFileName,
    string? OriginalFileName);
