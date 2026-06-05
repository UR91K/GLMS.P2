using System.Text.Json;
using GLMS.Shared.DTOs;

namespace GLMS.Api.Services.Currency;

public class ExchangeRateApiService : ICurrencyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExchangeRateApiService> _logger;

    public ExchangeRateApiService(HttpClient httpClient, ILogger<ExchangeRateApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CurrencyRateDto> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency))
            throw new ArgumentException("Source currency is required.", nameof(fromCurrency));
        if (string.IsNullOrWhiteSpace(toCurrency))
            throw new ArgumentException("Target currency is required.", nameof(toCurrency));

        var from = fromCurrency.Trim().ToUpperInvariant();
        var to = toCurrency.Trim().ToUpperInvariant();

        try
        {
            using var response = await _httpClient.GetAsync($"latest/{from}", cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

            if (!json.RootElement.TryGetProperty("conversion_rates", out var ratesElement))
                throw new CurrencyServiceException("Currency API response is missing rates data.");

            if (!ratesElement.TryGetProperty(to, out var targetRateElement))
                throw new CurrencyServiceException($"Currency API did not provide a rate for {to}.");

            return new CurrencyRateDto(targetRateElement.GetDecimal(), DateTime.UtcNow, false);
        }
        catch (CurrencyServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve exchange rate from {From} to {To}", from, to);
            throw new CurrencyServiceException("Unable to retrieve exchange rate right now. Please try again shortly.", ex);
        }
    }
}
