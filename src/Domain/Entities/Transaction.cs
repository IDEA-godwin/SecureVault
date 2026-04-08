
namespace SecureVault.Domain.Entities;

public class Transactions : BaseEntity
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public Guid AccountTo { get; set; }
    public Account To { get; set; } = null!;

    public Guid AccountFrom { get; set; }
    public Account From { get; set; } = null!;
}