using GLMS.Web.Models.Enums;

namespace GLMS.Web.DTOs;

public enum ContractTransitionAction
{
    Approve,
    Suspend,
    Resume,
    Expire
}

public sealed record ContractTransitionResultDto(
    bool Succeeded,
    int ContractId,
    ContractStatus PreviousStatus,
    ContractStatus CurrentStatus,
    string Message);
