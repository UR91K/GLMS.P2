namespace GLMS.Api.Services.Currency;

public class CurrencyServiceException : Exception
{
    public CurrencyServiceException(string message) : base(message) { }
    public CurrencyServiceException(string message, Exception innerException) : base(message, innerException) { }
}
