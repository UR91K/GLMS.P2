namespace GLMS.Web.Patterns.Observer;

public interface IContractObserver
{
    void OnContractStatusChanged(ContractStatusChangedEvent e);
}
