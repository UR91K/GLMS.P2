using GLMS.Shared.DTOs;

namespace GLMS.Api.Services.ServiceRequests;

public interface IServiceRequestService
{
    Task<IReadOnlyList<ServiceRequestListItemDto>> GetByContractAsync(int contractId, CancellationToken cancellationToken = default);
    Task<ServiceRequestCreationResult> CreateAsync(ServiceRequestCreateCommand command, CancellationToken cancellationToken = default);
}
