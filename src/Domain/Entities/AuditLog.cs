namespace SecureVault.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    public string FromAccountNumber { get; set; } = string.Empty;
    public string ToAccountNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public DateTime AuditTime { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
