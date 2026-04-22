using GLMS.Web.DTOs;

namespace GLMS.Web.Services.Contracts;

public interface IContractService
{
    Task<IReadOnlyList<ContractListItemDto>> GetListAsync(CancellationToken cancellationToken = default);
    Task<ContractTransitionResultDto> ChangeStatusAsync(int contractId, ContractTransitionAction action, CancellationToken cancellationToken = default);
}
