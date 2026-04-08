using SecureVault.Application.Common.Interfaces;

namespace SecureVault.Infrastructure.Services;

public class AccountNumberGenerator : IAccountNumberGenerator
{
    private readonly object _lock = new();
    private readonly Random _random = new();

    public string GenerateAccountNumber()
    {
        lock (_lock)
        {
            // Generate a unique 10-digit account number
            return _random.Next(1000000000, int.MaxValue).ToString()[0..10];
        }
    }
}
