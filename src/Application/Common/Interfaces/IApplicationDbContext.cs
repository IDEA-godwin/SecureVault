
using SecureVault.Domain.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SecureVault.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<AuditLog> AuditLogs { get; }
    
    DatabaseFacade Database { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
