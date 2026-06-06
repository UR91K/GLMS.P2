using GLMS.Shared.Enums;

namespace GLMS.Shared.DTOs;

public sealed record ServiceRequestCreateCommand(int ContractId, string Description, decimal CostUsd);

public sealed record ServiceRequestCreationResult(
    int ServiceRequestId,
    decimal ExchangeRate,
    decimal CostUsd,
    decimal CostZar,
    DateTime CreatedAtUtc,
    bool ExchangeRateFromCache);

public sealed record ServiceRequestListItemDto(
    int ServiceRequestId,
    int ContractId,
    string Description,
    decimal CostUsd,
    decimal CostZar,
    decimal ExchangeRate,
    ServiceRequestStatus Status,
    DateTime CreatedAt,
    string ContractTitle,
    string ClientName);
