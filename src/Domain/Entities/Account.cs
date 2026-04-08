
namespace SecureVault.Domain.Entities;

public class Account : BaseEntity
{
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public Guid UserId { get; set; }
}