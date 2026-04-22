namespace GLMS.Web.Services.Contracts;

public interface IContractService
{
    Task<IReadOnlyList<ContractListItemDto>> GetListAsync(CancellationToken cancellationToken = default);
}
