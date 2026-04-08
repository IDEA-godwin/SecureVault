namespace SecureVault.Application.Features.Transactions.Queries;

public class GetAuditLogsQuery : IRequest<PaginatedResult<AuditLogDto>>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string FromAccountNumber { get; set; } = string.Empty;
    public string ToAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime AuditTime { get; set; }
    public string? Notes { get; set; }
}

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedResult<AuditLogDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAuditLogsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        // Validate page parameters
        Guard.Against.NegativeOrZero(request.PageNumber, nameof(request.PageNumber));
        Guard.Against.NegativeOrZero(request.PageSize, nameof(request.PageSize));

        // Build query
        var query = _context.AuditLogs.AsQueryable();

        // Apply date filters
        if (request.StartDate.HasValue)
        {
            query = query.Where(x => x.AuditTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            var endDateUTC = request.EndDate.Value.AddDays(1); // Include entire end date
            query = query.Where(x => x.AuditTime < endDateUTC);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated audit logs
        var auditLogs = await query
            .Include(x => x.Transaction)
            .ThenInclude(x => x.FromAccount)
            .Include(x => x.Transaction)
            .ThenInclude(x => x.ToAccount)
            .OrderByDescending(x => x.AuditTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var auditLogDtos = auditLogs.Select(log => new AuditLogDto
        {
            Id = log.Id,
            TransactionId = log.TransactionId,
            FromAccountNumber = log.FromAccountNumber,
            ToAccountNumber = log.ToAccountNumber,
            Amount = log.Amount,
            AuditTime = log.AuditTime,
            Notes = log.Notes
        }).ToList();

        return new PaginatedResult<AuditLogDto>
        {
            Items = auditLogDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
