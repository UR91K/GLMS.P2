using GLMS.Api.Data;
using GLMS.Api.Models;
using GLMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Services;

public class DatabaseInitializationService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<DatabaseInitializationService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
            await db.Database.EnsureCreatedAsync(cancellationToken);
        else
            await db.Database.MigrateAsync(cancellationToken);

        if (await db.Clients.AnyAsync(cancellationToken))
            return;

        var today = DateTime.UtcNow.Date;

        var clients = new List<Client>
        {
            new()
            {
                Name = "Apex Logistics",
                Email = "operations@apexlogistics.co.za",
                Phone = "+27 11 458 2100",
                Region = "Gauteng",
                Contracts =
                [
                    new Contract
                    {
                        Title = "Fleet tracking and maintenance agreement",
                        ServiceLevel = "Gold",
                        StartDate = today.AddMonths(-5),
                        EndDate = today.AddMonths(7),
                        Status = ContractStatus.Active,
                        PdfFileName = "sample-apex-active.pdf",
                        PdfOriginalFileName = "Apex Fleet Agreement.pdf",
                        ServiceRequests =
                        [
                            new ServiceRequest { Description = "Replace damaged tracking unit on delivery vehicle", CostUsd = 125.00m, ExchangeRate = 18.42m, CostZar = 2302.50m, Status = ServiceRequestStatus.Open, CreatedAt = today.AddDays(-8) },
                            new ServiceRequest { Description = "Quarterly preventative maintenance visit", CostUsd = 210.00m, ExchangeRate = 18.15m, CostZar = 3811.50m, Status = ServiceRequestStatus.Completed, CreatedAt = today.AddDays(-22) }
                        ]
                    },
                    new Contract
                    {
                        Title = "Warehouse equipment support",
                        ServiceLevel = "Silver",
                        StartDate = today.AddMonths(-2),
                        EndDate = today.AddMonths(10),
                        Status = ContractStatus.OnHold,
                        PdfFileName = "sample-apex-hold.pdf",
                        PdfOriginalFileName = "Apex Warehouse Support.pdf"
                    }
                ]
            },
            new()
            {
                Name = "Northwind Retail Group",
                Email = "procurement@northwindretail.co.za",
                Phone = "+27 21 555 0184",
                Region = "Western Cape",
                Contracts =
                [
                    new Contract
                    {
                        Title = "Point-of-sale hardware lease",
                        ServiceLevel = "Platinum",
                        StartDate = today.AddMonths(-11),
                        EndDate = today.AddMonths(1),
                        Status = ContractStatus.Active,
                        PdfFileName = "sample-northwind-pos.pdf",
                        PdfOriginalFileName = "Northwind POS Lease.pdf",
                        ServiceRequests =
                        [
                            new ServiceRequest { Description = "Install replacement receipt printer", CostUsd = 95.00m, ExchangeRate = 18.33m, CostZar = 1741.35m, Status = ServiceRequestStatus.Completed, CreatedAt = today.AddDays(-14) }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "BluePeak Manufacturing",
                Email = "contracts@bluepeakmfg.com",
                Phone = "+27 31 804 9921",
                Region = "KwaZulu-Natal",
                Contracts =
                [
                    new Contract
                    {
                        Title = "Plant safety monitoring agreement",
                        ServiceLevel = "Bronze",
                        StartDate = today.AddMonths(-18),
                        EndDate = today.AddMonths(-1),
                        Status = ContractStatus.Expired,
                        PdfFileName = "sample-bluepeak-safety.pdf",
                        PdfOriginalFileName = "BluePeak Safety Monitoring.pdf",
                        ServiceRequests =
                        [
                            new ServiceRequest { Description = "Legacy sensor calibration request", CostUsd = 70.00m, ExchangeRate = 18.08m, CostZar = 1265.60m, Status = ServiceRequestStatus.Invalid, CreatedAt = today.AddDays(-41) }
                        ]
                    }
                ]
            },
            new()
            {
                Name = "Cedar Health Partners",
                Email = "facilities@cedarhealth.org",
                Phone = "+27 12 880 4477",
                Region = "Gauteng",
                Contracts =
                [
                    new Contract
                    {
                        Title = "Diagnostic equipment standby lease",
                        ServiceLevel = "Gold",
                        StartDate = today.AddDays(14),
                        EndDate = today.AddMonths(12),
                        Status = ContractStatus.Draft,
                        PdfFileName = "sample-cedar-draft.pdf",
                        PdfOriginalFileName = "Cedar Diagnostic Lease Draft.pdf"
                    }
                ]
            }
        };

        await db.Clients.AddRangeAsync(clients, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded GLMS demo data with {ClientCount} clients.", clients.Count);
    }
}
