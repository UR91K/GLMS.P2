using GLMS.Shared.DTOs;
using GLMS.Shared.Enums;

namespace GLMS.Web.Services.Contracts;

public interface IContractService
{
    Task<IReadOnlyList<ContractListItemDto>> GetListAsync(CancellationToken cancellationToken = default);
    Task<ContractTransitionResultDto> ChangeStatusAsync(int contractId, ContractTransitionAction action, CancellationToken cancellationToken = default);
    Task<ContractAgreementUploadResultDto> UploadAgreementAsync(
        int contractId,
        string originalFileName,
        string? contentType,
        long fileSize,
        Stream fileStream,
        CancellationToken cancellationToken = default);
}
