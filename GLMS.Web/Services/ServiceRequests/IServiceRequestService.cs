using GLMS.Shared.DTOs;

namespace GLMS.Web.Services.ServiceRequests;

public interface IServiceRequestService
{
    Task<IReadOnlyList<ServiceRequestListItemDto>> GetListAsync(CancellationToken cancellationToken = default);
    Task<ServiceRequestCreationResult> CreateAsync(ServiceRequestCreateCommand command, CancellationToken cancellationToken = default);
}
