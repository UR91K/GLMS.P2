using GLMS.Shared.DTOs;
using GLMS.Shared.Enums;

namespace GLMS.Api.Services.Contracts;

public interface IContractService
{
    Task<IReadOnlyList<ContractListItemDto>> GetListAsync(ContractStatus? status = null, int? clientId = null, CancellationToken cancellationToken = default);
    Task<ContractListItemDto?> GetByIdAsync(int contractId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ContractCreateCommand command, CancellationToken cancellationToken = default);
    Task<ContractTransitionResultDto> ChangeStatusAsync(int contractId, ContractTransitionAction action, CancellationToken cancellationToken = default);
    Task<ContractAgreementUploadResultDto> UploadAgreementAsync(int contractId, string originalFileName, string? contentType, long fileSize, Stream fileStream, CancellationToken cancellationToken = default);
}
