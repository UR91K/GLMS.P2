using GLMS.Web.Models;
using GLMS.Web.Models.Enums;

namespace GLMS.Web.Patterns.State;

public class DraftState : IContractState
{
    public void Approve(Contract contract) => contract.Status = ContractStatus.Active;
    public void Suspend(Contract contract) {  } // cannot suspend a draft
    public void Expire(Contract contract) => contract.Status = ContractStatus.Expired;
    public void Resume(Contract contract) {  } // cannot resume a draft
    public bool CanRaiseServiceRequest() => false;
}
