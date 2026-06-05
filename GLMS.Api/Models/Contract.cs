using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GLMS.Api.Patterns.Observer;
using GLMS.Api.Patterns.State;
using GLMS.Shared.Enums;

namespace GLMS.Api.Models;

public class Contract
{
    public int ContractId { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    [Required, StringLength(80)]
    public string ServiceLevel { get; set; } = string.Empty;

    public string? PdfFileName { get; set; }
    public string? PdfOriginalFileName { get; set; }

    public List<ServiceRequest> ServiceRequests { get; set; } = [];

    [NotMapped]
    public IContractState CurrentState => ContractStateFactory.Create(Status);

    public bool CanRaiseServiceRequest() => CurrentState.CanRaiseServiceRequest();

    public void Approve()
    {
        CurrentState.Approve(this);
        Notify(new ContractStatusChangedEvent(ContractId, Status.ToString()));
    }

    public void Suspend()
    {
        CurrentState.Suspend(this);
        Notify(new ContractStatusChangedEvent(ContractId, Status.ToString()));
    }

    public void Expire()
    {
        CurrentState.Expire(this);
        Notify(new ContractStatusChangedEvent(ContractId, Status.ToString()));
    }

    public void Resume()
    {
        CurrentState.Resume(this);
        Notify(new ContractStatusChangedEvent(ContractId, Status.ToString()));
    }

    private readonly List<IContractObserver> _observers = [];

    public void Subscribe(IContractObserver observer) => _observers.Add(observer);
    public void Unsubscribe(IContractObserver observer) => _observers.Remove(observer);

    private void Notify(ContractStatusChangedEvent e)
    {
        foreach (var observer in _observers)
            observer.OnContractStatusChanged(e);
    }
}
