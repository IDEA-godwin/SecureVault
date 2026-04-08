using System.Text.Json;
using SecureVault.Application.Common.Interfaces;
using SecureVault.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace SecureVault.Infrastructure.Services;

public class AuditLoggingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditLoggingBackgroundService> _logger;
    private readonly string _auditLogPath;

    public AuditLoggingBackgroundService(IServiceProvider serviceProvider, ILogger<AuditLoggingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Set up audit log file path
        var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs", "audit");
        Directory.CreateDirectory(logDirectory);
        _auditLogPath = Path.Combine(logDirectory, $"audit-{DateTime.UtcNow:yyyy-MM-dd}.log");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit Logging Background Service started at {Time}", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await LogRecentSuccessfulTransfersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the Audit Logging Background Service");
            }

            // Run every 5 seconds
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("Audit Logging Background Service stopped at {Time}", DateTime.UtcNow);
    }

    private async Task LogRecentSuccessfulTransfersAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            try
            {
                // Get recent successful transactions that haven't been logged yet (check by looking for those without audit logs)
                var recentTransactions = await context.Transactions
                    .Include(x => x.FromAccount)
                    .Include(x => x.ToAccount)
                    .Where(x => x.Status == Domain.Enums.TransactionStatus.Completed)
                    .OrderByDescending(x => x.TransactionDate)
                    .Take(10)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                foreach (var transaction in recentTransactions)
                {
                    // Check if this transaction has already been logged
                    var existingAuditLog = await context.AuditLogs
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.TransactionId == transaction.Id, cancellationToken);

                    if (existingAuditLog == null)
                    {
                        // Create audit log entry in database
                        var auditLog = new AuditLog
                        {
                            TransactionId = transaction.Id,
                            FromAccountNumber = transaction.FromAccount?.AccountNumber ?? string.Empty,
                            ToAccountNumber = transaction.ToAccount?.AccountNumber ?? string.Empty,
                            Amount = transaction.Amount,
                            AuditTime = DateTime.UtcNow,
                            Notes = $"Transfer from {transaction.FromAccount?.AccountNumber} to {transaction.ToAccount?.AccountNumber}. Description: {transaction.Description}"
                        };
                        context.AuditLogs.Add(auditLog);
                        await context.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation("Audit log created for transaction {TransactionId}", transaction.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LogRecentSuccessfulTransfersAsync");
            }
        }
    }
}
