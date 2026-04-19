using GLMS.Web.Models;
using GLMS.Web.Models.Enums;

namespace GLMS.Web.Patterns.State;

public class OnHoldState : IContractState
{
    public void Approve(Contract contract) {  } // do nothing, must be resumed first
    public void Suspend(Contract contract) {  } // already on hold
    public void Expire(Contract contract) => contract.Status = ContractStatus.Expired;
    public void Resume(Contract contract) => contract.Status = ContractStatus.Active;
    public bool CanRaiseServiceRequest() => false;
}
