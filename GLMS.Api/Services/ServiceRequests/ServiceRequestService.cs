using GLMS.Api.Data;
using GLMS.Api.Models;
using GLMS.Api.Services.Currency;
using GLMS.Shared.DTOs;
using GLMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Services.ServiceRequests;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ICurrencyService _currencyService;

    public ServiceRequestService(IDbContextFactory<AppDbContext> dbContextFactory, ICurrencyService currencyService)
    {
        _dbContextFactory = dbContextFactory;
        _currencyService = currencyService;
    }

    public async Task<IReadOnlyList<ServiceRequestListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ServiceRequests
            .AsNoTracking()
            .OrderByDescending(sr => sr.CreatedAt)
            .Select(sr => new ServiceRequestListItemDto(
                sr.ServiceRequestId, sr.ContractId, sr.Description,
                sr.CostUsd, sr.CostZar, sr.ExchangeRate, sr.Status, sr.CreatedAt,
                sr.Contract.Title, sr.Contract.Client.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequestListItemDto>> GetByContractAsync(int contractId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ServiceRequests
            .AsNoTracking()
            .Where(sr => sr.ContractId == contractId)
            .OrderByDescending(sr => sr.CreatedAt)
            .Select(sr => new ServiceRequestListItemDto(
                sr.ServiceRequestId, sr.ContractId, sr.Description,
                sr.CostUsd, sr.CostZar, sr.ExchangeRate, sr.Status, sr.CreatedAt,
                sr.Contract.Title, sr.Contract.Client.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceRequestCreationResult> CreateAsync(ServiceRequestCreateCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ContractId <= 0)
            throw new ArgumentException("A valid contract is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Description))
            throw new ArgumentException("Description is required.", nameof(command));
        if (command.CostUsd <= 0m)
            throw new ArgumentException("Cost in USD must be greater than zero.", nameof(command));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var contract = await db.Contracts.SingleOrDefaultAsync(c => c.ContractId == command.ContractId, cancellationToken)
            ?? throw new InvalidOperationException("The selected contract no longer exists.");

        if (!contract.CanRaiseServiceRequest())
            throw new InvalidOperationException($"Service requests cannot be created while this contract is {contract.Status}.");

        var rate = await _currencyService.GetRateAsync("USD", "ZAR", cancellationToken);
        var roundedUsd = decimal.Round(command.CostUsd, 2, MidpointRounding.AwayFromZero);
        var convertedZar = decimal.Round(roundedUsd * rate.Rate, 2, MidpointRounding.AwayFromZero);

        var request = new ServiceRequest
        {
            ContractId = command.ContractId,
            Description = command.Description.Trim(),
            CostUsd = roundedUsd,
            ExchangeRate = rate.Rate,
            CostZar = convertedZar,
            Status = ServiceRequestStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        await db.ServiceRequests.AddAsync(request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new ServiceRequestCreationResult(
            request.ServiceRequestId, request.ExchangeRate,
            request.CostUsd, request.CostZar, request.CreatedAt, rate.FromCache);
    }
}
