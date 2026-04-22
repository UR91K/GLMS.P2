namespace GLMS.Web.DTOs;

public sealed record ContractAgreementUploadResultDto(
    bool Succeeded,
    int ContractId,
    string Message,
    string? StoredFileName,
    string? OriginalFileName);
