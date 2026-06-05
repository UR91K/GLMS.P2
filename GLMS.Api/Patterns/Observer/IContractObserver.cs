namespace GLMS.Api.Patterns.Observer;

public interface IContractObserver
{
    void OnContractStatusChanged(ContractStatusChangedEvent e);
}
