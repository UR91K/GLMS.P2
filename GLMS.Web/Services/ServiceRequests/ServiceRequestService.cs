using GLMS.Web.Data;
using GLMS.Web.Models;
using GLMS.Web.Models.Enums;
using GLMS.Web.Services.Currency;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Web.Services.ServiceRequests;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ICurrencyService _currencyService;

    public ServiceRequestService(IDbContextFactory<AppDbContext> dbContextFactory, ICurrencyService currencyService)
    {
        _dbContextFactory = dbContextFactory;
        _currencyService = currencyService;
    }

    public async Task<ServiceRequestCreationResult> CreateAsync(ServiceRequestCreateCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ContractId <= 0)
        {
            throw new ArgumentException("A valid contract is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            throw new ArgumentException("Description is required.", nameof(command));
        }

        if (command.CostUsd <= 0m)
        {
            throw new ArgumentException("Cost in USD must be greater than zero.", nameof(command));
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var contract = await dbContext.Contracts
            .SingleOrDefaultAsync(item => item.ContractId == command.ContractId, cancellationToken);

        if (contract is null)
        {
            throw new InvalidOperationException("The selected contract no longer exists.");
        }

        if (!contract.CanRaiseServiceRequest())
        {
            throw new InvalidOperationException($"Service requests cannot be created while this contract is {contract.Status}.");
        }

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

        await dbContext.ServiceRequests.AddAsync(request, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ServiceRequestCreationResult(
            request.ServiceRequestId,
            request.ExchangeRate,
            request.CostUsd,
            request.CostZar,
            request.CreatedAt,
            rate.FromCache);
    }
}
