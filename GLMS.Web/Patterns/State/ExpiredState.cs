using GLMS.Web.Models;

namespace GLMS.Web.Patterns.State;

public class ExpiredState : IContractState
{
    public void Approve(Contract contract) {  } // cannot approve an expired contract
    public void Suspend(Contract contract) {  } // cannot suspend an expired contract
    public void Expire(Contract contract) {  } // already expired
    public void Resume(Contract contract) {  } // cannot resume an expired contract
    public bool CanRaiseServiceRequest() => false;
}
