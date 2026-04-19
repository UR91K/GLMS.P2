namespace GLMS.Web.Services.Currency.Models;

public sealed record CurrencyRateDto(decimal Rate, DateTime FetchedAtUtc, bool FromCache);
