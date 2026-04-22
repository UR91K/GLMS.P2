namespace GLMS.Web.DTOs;

public sealed record CurrencyRateDto(decimal Rate, DateTime FetchedAtUtc, bool FromCache);
