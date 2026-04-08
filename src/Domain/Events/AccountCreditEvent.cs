
namespace SecureVault.Domain.Events;

public class AccountCreditEvent : BaseEvent
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
}