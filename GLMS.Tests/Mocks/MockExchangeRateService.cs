using GLMS.Web.Services.Currency;

namespace GLMS.Tests.Mocks;

public class MockExchangeRateService : GLMS.Web.Services.Currency.ICurrencyService, GLMS.Api.Services.Currency.ICurrencyService
{
    private readonly Dictionary<(string From, string To), decimal> _rates = new();

    public int CallCount { get; private set; }

    public void SetRate(string fromCurrency, string toCurrency, decimal rate)
    {
        var key = Normalize(fromCurrency, toCurrency);
        _rates[key] = rate;
    }

    Task<GLMS.Web.DTOs.CurrencyRateDto> GLMS.Web.Services.Currency.ICurrencyService.GetRateAsync(
        string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        var rate = GetRateInternal(fromCurrency, toCurrency);
        return Task.FromResult(new GLMS.Web.DTOs.CurrencyRateDto(rate, DateTime.UtcNow, false));
    }

    Task<GLMS.Shared.DTOs.CurrencyRateDto> GLMS.Api.Services.Currency.ICurrencyService.GetRateAsync(
        string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        var rate = GetRateInternal(fromCurrency, toCurrency);
        return Task.FromResult(new GLMS.Shared.DTOs.CurrencyRateDto(rate, DateTime.UtcNow, false));
    }

    private decimal GetRateInternal(string fromCurrency, string toCurrency)
    {
        CallCount++;
        var key = Normalize(fromCurrency, toCurrency);
        if (!_rates.TryGetValue(key, out var rate))
            throw new CurrencyServiceException($"No mock rate configured for {key.From}->{key.To}.");
        return rate;
    }

    private static (string From, string To) Normalize(string fromCurrency, string toCurrency)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency))
            throw new ArgumentException("Source currency is required.", nameof(fromCurrency));
        if (string.IsNullOrWhiteSpace(toCurrency))
            throw new ArgumentException("Target currency is required.", nameof(toCurrency));
        return (fromCurrency.Trim().ToUpperInvariant(), toCurrency.Trim().ToUpperInvariant());
    }
}
