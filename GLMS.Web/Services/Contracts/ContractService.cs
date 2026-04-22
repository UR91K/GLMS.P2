using GLMS.Web.Data;
using GLMS.Web.DTOs;
using GLMS.Web.Models.Enums;
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

    public async Task<ContractTransitionResultDto> ChangeStatusAsync(int contractId, ContractTransitionAction action, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
        {
            throw new ArgumentException("A valid contract is required.", nameof(contractId));
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var contract = await dbContext.Contracts
            .SingleOrDefaultAsync(item => item.ContractId == contractId, cancellationToken);

        if (contract is null)
        {
            return new ContractTransitionResultDto(
                false,
                contractId,
                default,
                default,
                "The selected contract no longer exists.");
        }

        var previousStatus = contract.Status;

        switch (action)
        {
            case ContractTransitionAction.Approve:
                contract.Approve();
                break;
            case ContractTransitionAction.Suspend:
                contract.Suspend();
                break;
            case ContractTransitionAction.Resume:
                contract.Resume();
                break;
            case ContractTransitionAction.Expire:
                contract.Expire();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported contract transition action.");
        }

        if (contract.Status == previousStatus)
        {
            return new ContractTransitionResultDto(
                false,
                contract.ContractId,
                previousStatus,
                contract.Status,
                BuildInvalidTransitionMessage(previousStatus, action));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ContractTransitionResultDto(
            true,
            contract.ContractId,
            previousStatus,
            contract.Status,
            $"Contract CON-{contract.ContractId:D5} moved from {previousStatus} to {contract.Status}.");
    }

    private static string BuildInvalidTransitionMessage(ContractStatus currentStatus, ContractTransitionAction action)
    {
        return action switch
        {
            ContractTransitionAction.Approve when currentStatus == ContractStatus.OnHold => "On-hold contracts must be resumed before they can be approved.",
            ContractTransitionAction.Approve when currentStatus == ContractStatus.Expired => "Expired contracts cannot be approved.",
            ContractTransitionAction.Suspend when currentStatus == ContractStatus.Draft => "Draft contracts cannot be suspended. Approve the contract first.",
            ContractTransitionAction.Suspend when currentStatus == ContractStatus.Expired => "Expired contracts cannot be suspended.",
            ContractTransitionAction.Resume when currentStatus == ContractStatus.Draft => "Draft contracts cannot be resumed.",
            ContractTransitionAction.Resume when currentStatus == ContractStatus.Active => "This contract is already active.",
            ContractTransitionAction.Resume when currentStatus == ContractStatus.Expired => "Expired contracts cannot be resumed.",
            ContractTransitionAction.Expire when currentStatus == ContractStatus.Expired => "This contract is already expired.",
            _ => $"Cannot {action.ToString().ToLowerInvariant()} a contract while it is {currentStatus}."
        };
    }
}
