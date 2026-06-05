namespace GLMS.Shared.DTOs;

public sealed record CurrencyRateDto(decimal Rate, DateTime FetchedAtUtc, bool FromCache);
