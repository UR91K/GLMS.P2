using GLMS.Web.Services.Currency.Models;

namespace GLMS.Web.Services.Currency;

public interface ICurrencyService
{
    Task<CurrencyRateDto> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);
}
