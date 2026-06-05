using GLMS.Api.Models;
using GLMS.Shared.Enums;

namespace GLMS.Api.Patterns.State;

public class DraftState : IContractState
{
    public void Approve(Contract contract) => contract.Status = ContractStatus.Active;
    public void Suspend(Contract contract) { }
    public void Expire(Contract contract) => contract.Status = ContractStatus.Expired;
    public void Resume(Contract contract) { }
    public bool CanRaiseServiceRequest() => false;
}
