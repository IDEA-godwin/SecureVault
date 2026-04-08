using SecureVault.Application.Common;
using AppNotFoundException = SecureVault.Application.Common.Exceptions.NotFoundException;

namespace SecureVault.Application.Features.Transactions.Queries;

public class GetTransactionHistoryQuery : IRequest<PaginatedResult<TransactionDto>>
{
    public string AccountNumber { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetTransactionHistoryQueryHandler : IRequestHandler<GetTransactionHistoryQuery, PaginatedResult<TransactionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTransactionHistoryQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
    }

    public async Task<PaginatedResult<TransactionDto>> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
    {
        // Validate account exists
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountNumber == request.AccountNumber, cancellationToken);
        if (account is null)
        {
            throw new AppNotFoundException($"[{ErrorCodes.AccountNotFound}] Account with number {request.AccountNumber} not found");
        }
        Guard.Against.NegativeOrZero(request.PageNumber, nameof(request.PageNumber));
        Guard.Against.NegativeOrZero(request.PageSize, nameof(request.PageSize));

        var totalCount = await _context.Transactions
            .Where(x => x.FromAccountNumber == request.AccountNumber || x.ToAccountNumber == request.AccountNumber)
            .CountAsync(cancellationToken);

        var transactions = await _context.Transactions
            .Where(x => x.FromAccountNumber == request.AccountNumber || x.ToAccountNumber == request.AccountNumber)
            .Include(x => x.FromAccount)
            .Include(x => x.ToAccount)
            .OrderByDescending(x => x.TransactionDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var transactionDtos = transactions.Select(t => new TransactionDto
        {
            Id = t.Id,
            Description = t.Description!,
            Amount = t.Amount,
            TransactionDate = t.TransactionDate,
            TransactionType = account.AccountNumber == t.FromAccountNumber && account.AccountNumber == t.ToAccountNumber 
                ? "Credit" : account.AccountNumber == t.FromAccountNumber ? "Debit" : "Credit",
            Status = t.Status.ToString(),
            FromAccountNumber = t.FromAccountNumber,
            FromAccountName = t.FromAccount.Name,
            ToAccountNumber = t.ToAccountNumber,
            ToAccountName = t.ToAccount.Name,
            FailureReason = t.FailureReason
        }).ToList();

        return new PaginatedResult<TransactionDto>
        {
            Items = transactionDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
