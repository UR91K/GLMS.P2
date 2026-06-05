using GLMS.Shared.Enums;

namespace GLMS.Api.Models;

public class ServiceRequest
{
    public int ServiceRequestId { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal CostUsd { get; set; }
    public decimal CostZar { get; set; }
    public decimal ExchangeRate { get; set; }

    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
