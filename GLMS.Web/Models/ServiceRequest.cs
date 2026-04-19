using GLMS.Web.Models.Enums;

namespace GLMS.Web.Models;

public class ServiceRequest
{
    public int ServiceRequestId { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    /// <summary>amount in USD as entered by the user</summary>
    public decimal CostUsd { get; set; }

    /// <summary>converted amount in ZAR at the time of creation</summary>
    public decimal CostZar { get; set; }

    /// <summary>exchange rate (USD to ZAR) applied at creation time</summary>
    public decimal ExchangeRate { get; set; }

    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
