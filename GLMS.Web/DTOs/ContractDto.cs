using GLMS.Web.Models.Enums;

namespace GLMS.Web.DTOs;

public sealed record ContractListItemDto(
    int ContractId,
    int ClientId,
    string ClientName,
    string Title,
    string ServiceLevel,
    DateTime StartDate,
    DateTime EndDate,
    ContractStatus Status,
    int ServiceRequestCount,
    string? PdfFileName,
    string? PdfOriginalFileName);
