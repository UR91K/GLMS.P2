using GLMS.Web.Services.Currency.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GLMS.Web.Services.Currency;

public class CurrencyServiceProxy : ICurrencyService
{
    private readonly ExchangeRateApiService _inner;
    private readonly IMemoryCache _cache;
    private readonly int _ttlMinutes;

    public CurrencyServiceProxy(ExchangeRateApiService inner, IMemoryCache cache, IConfiguration configuration)
    {
        _inner = inner;
        _cache = cache;
        _ttlMinutes = configuration.GetValue<int?>("Currency:CacheTtlMinutes") ?? 15;
    }

    public async Task<CurrencyRateDto> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency))
        {
            throw new ArgumentException("Source currency is required.", nameof(fromCurrency));
        }

        if (string.IsNullOrWhiteSpace(toCurrency))
        {
            throw new ArgumentException("Target currency is required.", nameof(toCurrency));
        }

        var from = fromCurrency.Trim().ToUpperInvariant();
        var to = toCurrency.Trim().ToUpperInvariant();
        var cacheKey = $"currency:{from}:{to}";

        if (_cache.TryGetValue(cacheKey, out CachedRate? cachedRate) && cachedRate is not null)
        {
            return new CurrencyRateDto(cachedRate.Rate, cachedRate.FetchedAtUtc, true);
        }

        var freshRate = await _inner.GetRateAsync(from, to, cancellationToken);

        _cache.Set(cacheKey, new CachedRate(freshRate.Rate, freshRate.FetchedAtUtc), TimeSpan.FromMinutes(_ttlMinutes));

        return freshRate;
    }

    private sealed record CachedRate(decimal Rate, DateTime FetchedAtUtc);
}
