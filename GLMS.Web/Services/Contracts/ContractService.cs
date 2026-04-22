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
    private const long MaxAgreementFileBytes = 10 * 1024 * 1024;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IWebHostEnvironment _hostEnvironment;

    public ContractService(IDbContextFactory<AppDbContext> dbContextFactory, IWebHostEnvironment hostEnvironment)
    {
        _dbContextFactory = dbContextFactory;
        _hostEnvironment = hostEnvironment;
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

    public async Task<ContractAgreementUploadResultDto> UploadAgreementAsync(
        int contractId,
        string originalFileName,
        string? contentType,
        long fileSize,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
        {
            throw new ArgumentException("A valid contract is required.", nameof(contractId));
        }

        if (fileStream is null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return new ContractAgreementUploadResultDto(false, contractId, "Select a PDF file to upload.", null, null);
        }

        if (fileSize <= 0)
        {
            return new ContractAgreementUploadResultDto(false, contractId, "The selected file is empty.", null, null);
        }

        if (fileSize > MaxAgreementFileBytes)
        {
            return new ContractAgreementUploadResultDto(false, contractId, $"The PDF exceeds the 10 MB size limit.", null, null);
        }

        var extension = Path.GetExtension(originalFileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return new ContractAgreementUploadResultDto(false, contractId, "Only .pdf files are allowed for signed agreements.", null, null);
        }

        // MIME check to prevent spoofing, not entirely reliable but better than nothing
        if (!string.IsNullOrWhiteSpace(contentType) &&
            !string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return new ContractAgreementUploadResultDto(false, contractId, "The uploaded file must have a PDF content type.", null, null);
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var contract = await dbContext.Contracts
            .SingleOrDefaultAsync(item => item.ContractId == contractId, cancellationToken);

        if (contract is null)
        {
            return new ContractAgreementUploadResultDto(false, contractId, "The selected contract no longer exists.", null, null);
        }

        var webRoot = _hostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            throw new InvalidOperationException("Web root path is not configured.");
        }

        var uploadDirectory = Path.Combine(webRoot, "uploads", "contracts");
        Directory.CreateDirectory(uploadDirectory);

        var storedFileName = $"{Guid.NewGuid():N}.pdf";
        var storedPath = Path.Combine(uploadDirectory, storedFileName);

        await using (var targetStream = File.Create(storedPath))
        {
            await fileStream.CopyToAsync(targetStream, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(contract.PdfFileName))
        {
            var oldPath = Path.Combine(uploadDirectory, contract.PdfFileName);
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
        }

        contract.PdfFileName = storedFileName;
        contract.PdfOriginalFileName = Path.GetFileName(originalFileName);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ContractAgreementUploadResultDto(
            true,
            contract.ContractId,
            $"Signed agreement uploaded for CON-{contract.ContractId:D5}.",
            contract.PdfFileName,
            contract.PdfOriginalFileName);
    }
}
