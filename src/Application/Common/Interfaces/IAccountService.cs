using SecureVault.Application.Common.DTOs;

namespace SecureVault.Application.Common.Interfaces;

public interface IAccountService
{
    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="requestDto">The account creation request DTO containing Name and Email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created AccountDto</returns>
    Task<AccountDto> CreateAccountAsync(AccountRequestDto requestDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an account by its account number.
    /// </summary>
    /// <param name="accountNumber">The account number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The AccountDto if found, otherwise null</returns>
    Task<AccountDto?> GetAccountByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Credits (adds funds to) an account by account number and creates a credit transaction.
    /// </summary>
    /// <param name="accountNumber">The account number</param>
    /// <param name="amount">The amount to credit (must be positive)</param>
    /// <param name="description">Description of the credit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated AccountDto</returns>
    Task<AccountDto> CreditAccountByAccountNumberAsync(string accountNumber, decimal amount, string? description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all accounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of all AccountDtos</returns>
    Task<List<AccountDto>> GetAllAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an account by account number. Account must have zero balance.
    /// </summary>
    /// <param name="accountNumber">The account number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAccountByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
}
