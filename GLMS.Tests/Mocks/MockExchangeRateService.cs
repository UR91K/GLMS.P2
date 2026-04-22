using GLMS.Web.Services.Currency;
using GLMS.Web.DTOs;

namespace GLMS.Tests.Mocks;

public class MockExchangeRateService : ICurrencyService
{
    private readonly Dictionary<(string From, string To), decimal> _rates = new();

    public int CallCount { get; private set; }

    public void SetRate(string fromCurrency, string toCurrency, decimal rate)
    {
        var key = Normalize(fromCurrency, toCurrency);
        _rates[key] = rate;
    }

    public Task<CurrencyRateDto> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        CallCount++;

        var key = Normalize(fromCurrency, toCurrency);
        if (!_rates.TryGetValue(key, out var rate))
        {
            throw new CurrencyServiceException($"No mock rate configured for {key.From}->{key.To}.");
        }

        return Task.FromResult(new CurrencyRateDto(rate, DateTime.UtcNow, false));
    }

    private static (string From, string To) Normalize(string fromCurrency, string toCurrency)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency))
        {
            throw new ArgumentException("Source currency is required.", nameof(fromCurrency));
        }

        if (string.IsNullOrWhiteSpace(toCurrency))
        {
            throw new ArgumentException("Target currency is required.", nameof(toCurrency));
        }

        return (fromCurrency.Trim().ToUpperInvariant(), toCurrency.Trim().ToUpperInvariant());
    }
}
