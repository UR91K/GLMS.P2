using GLMS.Api.Models;
using GLMS.Shared.Enums;

namespace GLMS.Api.Patterns.State;

public class ActiveState : IContractState
{
    public void Approve(Contract contract) { }
    public void Suspend(Contract contract) => contract.Status = ContractStatus.OnHold;
    public void Expire(Contract contract) => contract.Status = ContractStatus.Expired;
    public void Resume(Contract contract) { }
    public bool CanRaiseServiceRequest() => true;
}
