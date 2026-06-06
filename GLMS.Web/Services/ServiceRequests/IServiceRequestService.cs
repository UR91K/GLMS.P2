namespace GLMS.Web.Services.ServiceRequests;

public interface IServiceRequestService
{
    Task<IReadOnlyList<GLMS.Shared.DTOs.ServiceRequestListItemDto>> GetListAsync(CancellationToken cancellationToken = default);
    Task<ServiceRequestCreationResult> CreateAsync(ServiceRequestCreateCommand command, CancellationToken cancellationToken = default);
}

public sealed record ServiceRequestCreateCommand(int ContractId, string Description, decimal CostUsd);

public sealed record ServiceRequestCreationResult(
    int ServiceRequestId,
    decimal ExchangeRate,
    decimal CostUsd,
    decimal CostZar,
    DateTime CreatedAtUtc,
    bool ExchangeRateFromCache);
