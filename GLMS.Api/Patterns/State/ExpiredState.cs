using GLMS.Api.Models;

namespace GLMS.Api.Patterns.State;

public class ExpiredState : IContractState
{
    public void Approve(Contract contract) { }
    public void Suspend(Contract contract) { }
    public void Expire(Contract contract) { }
    public void Resume(Contract contract) { }
    public bool CanRaiseServiceRequest() => false;
}
