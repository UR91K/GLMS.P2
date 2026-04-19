using GLMS.Web.Models;
using GLMS.Web.Models.Enums;

namespace GLMS.Web.Patterns.State;

public class ActiveState : IContractState
{
    public void Approve(Contract contract) {  } // already active
    public void Suspend(Contract contract) => contract.Status = ContractStatus.OnHold;
    public void Expire(Contract contract) => contract.Status = ContractStatus.Expired;
    public void Resume(Contract contract) {  } // already active
    public bool CanRaiseServiceRequest() => true;
}
