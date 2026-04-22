using GLMS.Web.Data;
using GLMS.Web.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Web.Services.Contracts;

/// <summary>
/// service for managing contracts.
/// 
/// moved out of the page for separation of concerns!
/// </summary>
public class ContractService : IContractService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ContractService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<ContractListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Contracts
            .AsNoTracking()
            .OrderBy(contract => contract.EndDate)
            .ThenBy(contract => contract.Title)
            .Select(contract => new ContractListItemDto(
                contract.ContractId,
                contract.ClientId,
                contract.Client.Name,
                contract.Title,
                contract.ServiceLevel,
                contract.StartDate,
                contract.EndDate,
                contract.Status,
                contract.ServiceRequests.Count(),
                contract.PdfFileName,
                contract.PdfOriginalFileName))
            .ToListAsync(cancellationToken);
    }
}
