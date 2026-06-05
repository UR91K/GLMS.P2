namespace GLMS.Api.Patterns.Observer;

public class ContractStatusChangedEvent
{
    public int ContractId { get; }
    public string NewStatus { get; }

    public ContractStatusChangedEvent(int contractId, string newStatus)
    {
        ContractId = contractId;
        NewStatus = newStatus;
    }
}
