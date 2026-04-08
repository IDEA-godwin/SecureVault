using SecureVault.Application.Common;
using AppNotFoundException = SecureVault.Application.Common.Exceptions.NotFoundException;
using SecureVault.Domain.Enums;

namespace SecureVault.Application.Features.Transactions.Commands;

public class TransferFundsCommand : IRequest<TransactionDto>
{
    public string FromAccountNumber { get; set; } = string.Empty;
    public string ToAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class TransferFundsCommandValidator : AbstractValidator<TransferFundsCommand>
{
    public TransferFundsCommandValidator()
    {
        RuleFor(x => x.FromAccountNumber)
            .NotEmpty().WithMessage("From account number must not be empty");

        RuleFor(x => x.ToAccountNumber)
            .NotEmpty().WithMessage("To account number must not be empty")
            .NotEqual(x => x.FromAccountNumber).WithMessage("Cannot transfer to the same account");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Transfer amount must be greater than 0");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("Description must not exceed 200 characters");
    }
}

public class TransferFundsCommandHandler : IRequestHandler<TransferFundsCommand, TransactionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public TransferFundsCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<TransactionDto> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        // Begin transaction
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            {
                var fromAccount = await _context.Accounts
                    .FirstOrDefaultAsync(x => x.AccountNumber == request.FromAccountNumber, cancellationToken)
                    ?? throw new AppNotFoundException($"[{ErrorCodes.AccountNotFound}] From account with number {request.FromAccountNumber} not found");

                var toAccount = await _context.Accounts
                    .FirstOrDefaultAsync(x => x.AccountNumber == request.ToAccountNumber, cancellationToken) 
                    ?? throw new AppNotFoundException($"[{ErrorCodes.AccountNotFound}] To account with number {request.ToAccountNumber} not found");
    
                try
                {
                    if (request.Amount <= 0)
                    {
                        throw new ArgumentException($"[{ErrorCodes.InvalidTransferAmount}] Transfer amount must be greater than zero");
                    }

                    if (fromAccount.Balance < request.Amount)
                    {
                        throw new InvalidOperationException($"[{ErrorCodes.InsufficientFunds}] Insufficient funds. Available balance: {fromAccount.Balance}, Transfer amount: {request.Amount}");
                    }

                    fromAccount.Balance -= request.Amount;
                    toAccount.Balance += request.Amount;
                    
                    var tx = new Transaction
                    {
                        FromAccountId = fromAccount.Id,
                        FromAccountNumber = fromAccount.AccountNumber,
                        ToAccountId = toAccount.Id,  // Self-reference for debit
                        ToAccountNumber = toAccount.AccountNumber,
                        Amount = request.Amount,
                        Description = request.Description,
                        TransactionDate = DateTime.UtcNow,
                        Status = TransactionStatus.Completed
                    };


                    _context.Transactions.Add(tx);
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return _mapper.Map<TransactionDto>(tx);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        });
    }
}
