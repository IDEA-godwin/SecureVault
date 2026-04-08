
using SecureVault.Domain.Enums;

namespace SecureVault.Domain.Entities;

public class Transaction : BaseAuditableEntity
{
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public Guid FromAccountId { get; set; }
    public string FromAccountNumber { get; set; } = null!;
    public Account FromAccount { get; set; } = null!;

    public Guid ToAccountId { get; set; }
    public string ToAccountNumber { get; set; } = null!;
    public Account ToAccount { get; set; } = null!;

    public string? FailureReason { get; set; }
}