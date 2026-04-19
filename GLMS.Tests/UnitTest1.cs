using GLMS.Tests.Mocks;
using GLMS.Web.Services.Currency;

namespace GLMS.Tests;

public class UnitTest1
{
    [Fact]
    public async Task MockExchangeRateService_ReturnsConfiguredRate()
    {
        var mockService = new MockExchangeRateService();
        mockService.SetRate("USD", "ZAR", 18.75m);

        var result = await mockService.GetRateAsync("usd", "zar");

        Assert.Equal(18.75m, result.Rate);
        Assert.False(result.FromCache);
        Assert.Equal(1, mockService.CallCount);
    }

    [Fact]
    public async Task MockExchangeRateService_ThrowsWhenRateMissing()
    {
        var mockService = new MockExchangeRateService();

        var ex = await Assert.ThrowsAsync<CurrencyServiceException>(() =>
            mockService.GetRateAsync("USD", "ZAR"));

        Assert.Contains("No mock rate configured", ex.Message);
        Assert.Equal(1, mockService.CallCount);
    }
}
