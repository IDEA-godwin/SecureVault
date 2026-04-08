
namespace SecureVault.Domain.Events;

public class AccountDebitEvent : BaseEvent
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
}