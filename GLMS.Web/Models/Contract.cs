using System.ComponentModel.DataAnnotations.Schema;
using GLMS.Web.Models.Enums;
using GLMS.Web.Patterns.Observer;
using GLMS.Web.Patterns.State;

namespace GLMS.Web.Models;

public class Contract
{
    public int ContractId { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(150)]
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(80)]
    public string ServiceLevel { get; set; } = string.Empty;

    public string? PdfFileName { get; set; }
    public string? PdfOriginalFileName { get; set; }

    public List<ServiceRequest> ServiceRequests { get; set; } = [];

    // state pattern
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

    // observer pattern
    private readonly List<IContractObserver> _observers = [];

    public void Subscribe(IContractObserver observer) => _observers.Add(observer);

    public void Unsubscribe(IContractObserver observer) => _observers.Remove(observer);

    private void Notify(ContractStatusChangedEvent e)
    {
        foreach (var observer in _observers)
            observer.OnContractStatusChanged(e);
    }
}
