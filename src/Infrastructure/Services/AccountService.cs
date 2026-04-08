using SecureVault.Application.Common;
using SecureVault.Application.Common.DTOs;
using SecureVault.Application.Common.Exceptions;
using SecureVault.Application.Common.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AppNotFoundException = SecureVault.Application.Common.Exceptions.NotFoundException;

namespace SecureVault.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAccountNumberGenerator _accountNumberGenerator;

    public AccountService(
        IApplicationDbContext context,
        IMapper mapper,
        IAccountNumberGenerator accountNumberGenerator)
    {
        _context = context;
        _mapper = mapper;
        _accountNumberGenerator = accountNumberGenerator;
    }

    public async Task<AccountDto> CreateAccountAsync(AccountRequestDto request, CancellationToken cancellationToken = default)
    {
        // Validate email is unique
        var existingAccount = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == request.Email, cancellationToken);
        
        if (existingAccount != null)
        {
            throw new InvalidOperationException($"[{ErrorCodes.DuplicateAccount}] An account with email {request.Email} already exists");
        }

        var account = new Account
        {
            Name = request.Name,
            Email = request.Email,
            AccountNumber = _accountNumberGenerator.GenerateAccountNumber(),
            Balance = 0
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<AccountDto>(account);
    }

    public async Task<AccountDto?> GetAccountByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountNumber == accountNumber, cancellationToken);

        return account != null ? _mapper.Map<AccountDto>(account) : null;
    }

    public async Task<AccountDto> CreditAccountByAccountNumberAsync(string accountNumber, decimal amount, string? description, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(accountNumber, nameof(accountNumber));
        if (amount <= 0)
        {
            throw new ArgumentException($"[{ErrorCodes.InvalidTransferAmount}] Credit amount must be greater than zero");
        }

        using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                var account = await _context.Accounts
                    .FirstOrDefaultAsync(x => x.AccountNumber == accountNumber, cancellationToken);
                if (account is null)
                {
                    throw new AppNotFoundException($"[{ErrorCodes.AccountNotFound}] Account with number {accountNumber} not found");
                }

                // Credit the account
                account.Balance += amount;

                // Create transaction record
                var transactionRecord = new Transaction
                {
                    FromAccountId = account.Id,
                    FromAccountNumber = account.AccountNumber,
                    ToAccountId = account.Id,
                    ToAccountNumber = account.AccountNumber,
                    Amount = amount,
                    Description = description,
                    TransactionDate = DateTime.UtcNow,
                    Type = TransactionType.Credit,
                    Status = TransactionStatus.Completed
                };

                _context.Accounts.Update(account);
                _context.Transactions.Add(transactionRecord);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return _mapper.Map<AccountDto>(account);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    public async Task<List<AccountDto>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _context.Accounts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return accounts.Select(a => _mapper.Map<AccountDto>(a)).ToList();
    }

    public async Task DeleteAccountByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(accountNumber, nameof(accountNumber));

        var account = await _context.Accounts
            .FirstOrDefaultAsync(x => x.AccountNumber == accountNumber, cancellationToken);
        if (account is null)
        {
            throw new AppNotFoundException($"[{ErrorCodes.AccountNotFound}] Account with number {accountNumber} not found");
        }

        // Prevent deletion if account has active balance
        if (account.Balance > 0)
        {
            throw new InvalidOperationException($"[{ErrorCodes.AccountHasBalance}] Cannot delete account with active balance. Current balance: {account.Balance}. Please withdraw all funds before deletion.");
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
