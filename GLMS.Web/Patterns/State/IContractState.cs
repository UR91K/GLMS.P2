using GLMS.Web.Models;

namespace GLMS.Web.Patterns.State;

public interface IContractState
{
    void Approve(Contract contract);
    void Suspend(Contract contract);
    void Expire(Contract contract);
    void Resume(Contract contract);
    bool CanRaiseServiceRequest();
}
