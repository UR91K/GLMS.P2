using GLMS.Shared.Enums;

namespace GLMS.Api.Patterns.State;

public static class ContractStateFactory
{
    public static IContractState Create(ContractStatus status) => status switch
    {
        ContractStatus.Draft   => new DraftState(),
        ContractStatus.Active  => new ActiveState(),
        ContractStatus.OnHold  => new OnHoldState(),
        ContractStatus.Expired => new ExpiredState(),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown contract status.")
    };
}
