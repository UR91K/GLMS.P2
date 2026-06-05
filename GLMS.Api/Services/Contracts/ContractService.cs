using GLMS.Api.Data;
using GLMS.Api.Models;
using GLMS.Shared.DTOs;
using GLMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Services.Contracts;

public class ContractService : IContractService
{
    private const long MaxAgreementFileBytes = 10 * 1024 * 1024;
    private static readonly byte[] PdfHeader = "%PDF"u8.ToArray();
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IWebHostEnvironment _hostEnvironment;

    public ContractService(IDbContextFactory<AppDbContext> dbContextFactory, IWebHostEnvironment hostEnvironment)
    {
        _dbContextFactory = dbContextFactory;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<IReadOnlyList<ContractListItemDto>> GetListAsync(ContractStatus? status = null, int? clientId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Contracts.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);
        if (clientId.HasValue)
            query = query.Where(c => c.ClientId == clientId.Value);

        return await query
            .OrderBy(c => c.EndDate)
            .ThenBy(c => c.Title)
            .Select(c => new ContractListItemDto(
                c.ContractId, c.ClientId, c.Client.Name, c.Title,
                c.ServiceLevel, c.StartDate, c.EndDate, c.Status,
                c.ServiceRequests.Count(), c.PdfFileName, c.PdfOriginalFileName))
            .ToListAsync(cancellationToken);
    }

    public async Task<ContractListItemDto?> GetByIdAsync(int contractId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Contracts
            .AsNoTracking()
            .Where(c => c.ContractId == contractId)
            .Select(c => new ContractListItemDto(
                c.ContractId, c.ClientId, c.Client.Name, c.Title,
                c.ServiceLevel, c.StartDate, c.EndDate, c.Status,
                c.ServiceRequests.Count(), c.PdfFileName, c.PdfOriginalFileName))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(ContractCreateCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
            throw new ArgumentException("Title is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.ServiceLevel))
            throw new ArgumentException("Service level is required.", nameof(command));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var contract = new Contract
        {
            ClientId = command.ClientId,
            Title = command.Title.Trim(),
            ServiceLevel = command.ServiceLevel.Trim(),
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Status = ContractStatus.Draft
        };
        await db.Contracts.AddAsync(contract, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return contract.ContractId;
    }

    public async Task<ContractTransitionResultDto> ChangeStatusAsync(int contractId, ContractTransitionAction action, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
            throw new ArgumentException("A valid contract is required.", nameof(contractId));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var contract = await db.Contracts.SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);

        if (contract is null)
            return new ContractTransitionResultDto(false, contractId, default, default, "The selected contract no longer exists.");

        var previousStatus = contract.Status;

        switch (action)
        {
            case ContractTransitionAction.Approve: contract.Approve(); break;
            case ContractTransitionAction.Suspend: contract.Suspend(); break;
            case ContractTransitionAction.Resume: contract.Resume(); break;
            case ContractTransitionAction.Expire: contract.Expire(); break;
            default: throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported contract transition action.");
        }

        if (contract.Status == previousStatus)
            return new ContractTransitionResultDto(false, contract.ContractId, previousStatus, contract.Status, BuildInvalidTransitionMessage(previousStatus, action));

        await db.SaveChangesAsync(cancellationToken);
        return new ContractTransitionResultDto(true, contract.ContractId, previousStatus, contract.Status, $"Contract CON-{contract.ContractId:D5} moved from {previousStatus} to {contract.Status}.");
    }

    private static string BuildInvalidTransitionMessage(ContractStatus currentStatus, ContractTransitionAction action) =>
        action switch
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

    public async Task<ContractAgreementUploadResultDto> UploadAgreementAsync(int contractId, string originalFileName, string? contentType, long fileSize, Stream fileStream, CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
            throw new ArgumentException("A valid contract is required.", nameof(contractId));
        if (fileStream is null)
            throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(originalFileName))
            return new ContractAgreementUploadResultDto(false, contractId, "Select a PDF file to upload.", null, null);
        if (fileSize <= 0)
            return new ContractAgreementUploadResultDto(false, contractId, "The selected file is empty.", null, null);
        if (fileSize > MaxAgreementFileBytes)
            return new ContractAgreementUploadResultDto(false, contractId, "The PDF exceeds the 10 MB size limit.", null, null);

        var extension = Path.GetExtension(originalFileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            return new ContractAgreementUploadResultDto(false, contractId, "Only .pdf files are allowed for signed agreements.", null, null);

        if (!string.IsNullOrWhiteSpace(contentType) && !string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return new ContractAgreementUploadResultDto(false, contractId, "The uploaded file must have a PDF content type.", null, null);

        await using var uploadBuffer = new MemoryStream();
        await fileStream.CopyToAsync(uploadBuffer, cancellationToken);

        if (!HasPdfMagicNumber(uploadBuffer))
            return new ContractAgreementUploadResultDto(false, contractId, "The uploaded file content is not a valid PDF document.", null, null);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var contract = await db.Contracts.SingleOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);

        if (contract is null)
            return new ContractAgreementUploadResultDto(false, contractId, "The selected contract no longer exists.", null, null);

        var webRoot = _hostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadDirectory = Path.Combine(webRoot, "uploads", "contracts");
        Directory.CreateDirectory(uploadDirectory);

        var storedFileName = $"{Guid.NewGuid():N}.pdf";
        var storedPath = Path.Combine(uploadDirectory, storedFileName);

        uploadBuffer.Position = 0;
        await using (var targetStream = File.Create(storedPath))
            await uploadBuffer.CopyToAsync(targetStream, cancellationToken);

        if (!string.IsNullOrWhiteSpace(contract.PdfFileName))
        {
            var oldPath = Path.Combine(uploadDirectory, contract.PdfFileName);
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }

        contract.PdfFileName = storedFileName;
        contract.PdfOriginalFileName = Path.GetFileName(originalFileName);
        await db.SaveChangesAsync(cancellationToken);

        return new ContractAgreementUploadResultDto(true, contract.ContractId, $"Signed agreement uploaded for CON-{contract.ContractId:D5}.", contract.PdfFileName, contract.PdfOriginalFileName);
    }

    private static bool HasPdfMagicNumber(Stream stream)
    {
        if (stream is null) return false;
        stream.Position = 0;
        Span<byte> header = stackalloc byte[4];
        var bytesRead = stream.Read(header);
        stream.Position = 0;
        return bytesRead == PdfHeader.Length && header.SequenceEqual(PdfHeader);
    }
}
