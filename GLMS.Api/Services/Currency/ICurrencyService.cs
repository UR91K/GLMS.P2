using GLMS.Shared.DTOs;

namespace GLMS.Api.Services.Currency;

public interface ICurrencyService
{
    Task<CurrencyRateDto> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);
}
