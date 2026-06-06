using GLMS.Tests.Mocks;
using GLMS.Tests.TestSupport;
using GLMS.Api.Data;
using GLMS.Api.Models;
using GLMS.Api.Services.ServiceRequests;
using GLMS.Shared.DTOs;
using GLMS.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Tests;

public class ServiceRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_ConvertsUsdToZar_AndPersistsOpenRequest()
    {
        var options = NewInMemoryOptions();

        await using (var seedDb = new AppDbContext(options))
        {
            seedDb.Clients.Add(new Client
            {
                ClientId = 1,
                Name = "Acme Logistics",
                Email = "ops@acme.test",
                Phone = "+27 11 000 0000",
                Region = "Gauteng"
            });

            seedDb.Contracts.Add(new Contract
            {
                ContractId = 100,
                ClientId = 1,
                Title = "Main Agreement",
                ServiceLevel = "Gold",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(365),
                Status = ContractStatus.Active
            });

            await seedDb.SaveChangesAsync();
        }

        var mockRates = new MockExchangeRateService();
        mockRates.SetRate("USD", "ZAR", 18.42m);

        var sut = new ServiceRequestService(new TestDbContextFactory(options), mockRates);

        var result = await sut.CreateAsync(new ServiceRequestCreateCommand(100, "Install replacement unit", 12.345m));

        Assert.Equal(18.42m, result.ExchangeRate);
        Assert.Equal(12.35m, result.CostUsd);
        Assert.Equal(227.49m, result.CostZar);

        await using var assertDb = new AppDbContext(options);
        var stored = await assertDb.ServiceRequests.SingleAsync();
        Assert.Equal(ServiceRequestStatus.Open, stored.Status);
        Assert.Equal("Install replacement unit", stored.Description);
        Assert.Equal(12.35m, stored.CostUsd);
        Assert.Equal(227.49m, stored.CostZar);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task CreateAsync_Throws_WhenUsdNotPositive(decimal usd)
    {
        var options = NewInMemoryOptions();
        var mockRates = new MockExchangeRateService();
        mockRates.SetRate("USD", "ZAR", 18m);
        var sut = new ServiceRequestService(new TestDbContextFactory(options), mockRates);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateAsync(new ServiceRequestCreateCommand(1, "Test", usd)));

        Assert.Contains("greater than zero", ex.Message);
    }

    [Theory]
    [InlineData(ContractStatus.OnHold)]
    [InlineData(ContractStatus.Expired)]
    public async Task CreateAsync_Throws_WhenContractCannotRaiseRequest(ContractStatus status)
    {
        var options = NewInMemoryOptions();

        await using (var seedDb = new AppDbContext(options))
        {
            seedDb.Clients.Add(new Client
            {
                ClientId = 2,
                Name = "Northwind",
                Email = "team@northwind.test",
                Phone = "+27 21 000 0000",
                Region = "Western Cape"
            });

            seedDb.Contracts.Add(new Contract
            {
                ContractId = 200,
                ClientId = 2,
                Title = "Blocked Contract",
                ServiceLevel = "Silver",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                Status = status
            });

            await seedDb.SaveChangesAsync();
        }

        var mockRates = new MockExchangeRateService();
        mockRates.SetRate("USD", "ZAR", 18m);
        var sut = new ServiceRequestService(new TestDbContextFactory(options), mockRates);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateAsync(new ServiceRequestCreateCommand(200, "Blocked", 100m)));

        Assert.Contains("cannot be created", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenDescriptionMissing()
    {
        var options = NewInMemoryOptions();
        var mockRates = new MockExchangeRateService();
        mockRates.SetRate("USD", "ZAR", 18m);
        var sut = new ServiceRequestService(new TestDbContextFactory(options), mockRates);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateAsync(new ServiceRequestCreateCommand(1, " ", 10m)));

        Assert.Contains("Description is required", ex.Message);
    }

    private static DbContextOptions<AppDbContext> NewInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
    }
}
