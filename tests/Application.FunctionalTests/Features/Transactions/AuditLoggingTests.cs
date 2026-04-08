using SecureVault.Application.Common.DTOs;
using SecureVault.Application.Common.Exceptions;
using SecureVault.Application.Features.Accounts.Commands;
using SecureVault.Application.Features.Accounts.Queries;
using SecureVault.Application.Features.Transactions.Commands;
using SecureVault.Application.Features.Transactions.Queries;
using AppNotFoundException = SecureVault.Application.Common.Exceptions.NotFoundException;

namespace SecureVault.Application.FunctionalTests.Features.Transactions;

/// <summary>
/// Functional tests for Audit Logging Background Service.
/// Tests: successful transfer logging, failed transfer non-logging, and audit data integrity.
/// </summary>
[TestFixture]
public class AuditLoggingTests : TestBase
{
    private AccountDto _fromAccount = null!;
    private AccountDto _toAccount = null!;

    [SetUp]
    public async Task AuditLoggingTestSetUp()
    {
        // Create two accounts for transfer testing
        var createFromCommand = new CreateAccountCommand
        {
            Name = "From Account",
            Email = "from@example.com"
        };

        var createToCommand = new CreateAccountCommand
        {
            Name = "To Account",
            Email = "to@example.com"
        };

        _fromAccount = await TestApp.SendAsync(createFromCommand);
        _toAccount = await TestApp.SendAsync(createToCommand);

        // Credit the from account with initial balance
        var creditCommand = new CreditAccountCommand
        {
            AccountNumber = _fromAccount.AccountNumber,
            Amount = 10000,
            Description = "Initial balance"
        };

        _fromAccount = await TestApp.SendAsync(creditCommand);
    }

    [Test]
    public async Task AuditLogCreated_WhenTransferSucceeds()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 500,
            Description = "Test transfer"
        };

        // Act
        var transaction = await TestApp.SendAsync(transferCommand);

        // Wait for background service to process (allow for async audit logging)
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Query audit logs
        var auditQuery = new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        var auditLogs = await TestApp.SendAsync(auditQuery);

        // Assert
        auditLogs.ShouldNotBeNull();
        auditLogs.Items.Count.ShouldBeGreaterThan(0);
        
        var auditLog = auditLogs.Items.FirstOrDefault(a => a.TransactionId == transaction.Id);
        auditLog.ShouldNotBeNull();
        auditLog!.FromAccountNumber.ShouldBe(_fromAccount.AccountNumber);
        auditLog.ToAccountNumber.ShouldBe(_toAccount.AccountNumber);
        auditLog.Amount.ShouldBe(500);
        auditLog.Notes.ShouldBe("Transfer logged via AuditLoggingMiddleware");
    }

    [Test]
    public async Task AuditLogNotCreated_WhenTransferFails()
    {
        // Arrange - Attempt transfer that will fail (insufficient funds)
        var failingCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 50000, // More than available
            Description = "This will fail"
        };

        // Act
        try
        {
            await TestApp.SendAsync(failingCommand);
        }
        catch (InvalidOperationException)
        {
            // Expected - insufficient funds
        }

        // Wait for background service
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Get audit logs and count before
        var auditQuery = new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = 100
        };

        var auditLogs = await TestApp.SendAsync(auditQuery);

        // Assert - No audit log should be created for failed transfer
        // The failed transfer shouldn't create a transaction record, so no audit log should exist
        auditLogs.Items.Count.ShouldBe(0);
    }

    [Test]
    public async Task AuditLog_ShouldContainCorrectTransferDetails()
    {
        // Arrange
        var amount = 1250.50m;
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = amount,
            Description = "Detailed transfer"
        };

        var transaction = await TestApp.SendAsync(transferCommand);

        // Wait for background service
        await Task.Delay(TimeSpan.FromSeconds(6));

        var auditQuery = new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        var auditLogs = await TestApp.SendAsync(auditQuery);

        // Assert
        var auditLog = auditLogs.Items.FirstOrDefault(a => a.TransactionId == transaction.Id);
        auditLog.ShouldNotBeNull();
        auditLog!.Id.ShouldNotBe(default);
        auditLog.TransactionId.ShouldBe(transaction.Id);
        auditLog.FromAccountNumber.ShouldBe(_fromAccount.AccountNumber);
        auditLog.ToAccountNumber.ShouldBe(_toAccount.AccountNumber);
        auditLog.Amount.ShouldBe(amount);
        auditLog.AuditTime.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-10));
    }

    [Test]
    public async Task AuditLog_ShouldRecordAuditTimeInUTC()
    {
        // Arrange
        var beforeTransfer = DateTime.UtcNow;
        
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 100,
            Description = "Time test"
        };

        var transaction = await TestApp.SendAsync(transferCommand);

        var afterTransfer = DateTime.UtcNow;

        // Wait for background service
        await Task.Delay(TimeSpan.FromSeconds(6));

        var auditQuery = new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        var auditLogs = await TestApp.SendAsync(auditQuery);

        // Assert
        var auditLog = auditLogs.Items.FirstOrDefault(a => a.TransactionId == transaction.Id);
        auditLog.ShouldNotBeNull();
        auditLog!.AuditTime.ShouldBeGreaterThanOrEqualTo(beforeTransfer);
        auditLog.AuditTime.ShouldBeLessThanOrEqualTo(afterTransfer.AddSeconds(10)); // Allow for processing time
    }

    [Test]
    public async Task AuditLogs_ShouldBePaginatedCorrectly()
    {
        // Arrange - Create multiple transfers
        for (int i = 0; i < 15; i++)
        {
            var transferCommand = new TransferFundsCommand
            {
                FromAccountNumber = _fromAccount.AccountNumber,
                ToAccountNumber = _toAccount.AccountNumber,
                Amount = 10,
                Description = $"Transfer {i+1}"
            };

            await TestApp.SendAsync(transferCommand);
        }

        // Wait for background service to process all transfers
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Act - Get first page
        var page1 = await TestApp.SendAsync(new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = 5
        });

        // Get second page
        var page2 = await TestApp.SendAsync(new GetAuditLogsQuery
        {
            PageNumber = 2,
            PageSize = 5
        });

        // Get third page
        var page3 = await TestApp.SendAsync(new GetAuditLogsQuery
        {
            PageNumber = 3,
            PageSize = 5
        });

        // Assert
        page1.Items.Count.ShouldBe(5);
        page2.Items.Count.ShouldBe(5);
        page3.Items.Count.ShouldBe(5);
        
        page1.TotalCount.ShouldBe(15);
        page2.TotalCount.ShouldBe(15);
        page3.TotalCount.ShouldBe(15);

        // Verify no overlap between pages
        var page1Ids = page1.Items.Select(a => a.Id).ToList();
        var page2Ids = page2.Items.Select(a => a.Id).ToList();
        
        page1Ids.Any(id => page2Ids.Contains(id)).ShouldBeFalse();
    }

    [Test]
    public async Task AuditLogs_ShouldBeOrderedByAuditTimeDescending()
    {
        // Arrange - Create multiple transfers
        var transfers = new List<TransactionDto>();
        
        for (int i = 0; i < 5; i++)
        {
            var transferCommand = new TransferFundsCommand
            {
                FromAccountNumber = _fromAccount.AccountNumber,
                ToAccountNumber = _toAccount.AccountNumber,
                Amount = 100,
                Description = $"Transfer {i+1}"
            };

            var result = await TestApp.SendAsync(transferCommand);
            transfers.Add(result);
        }

        // Wait for background service to process all transfers
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Act
        var auditLogs = await TestApp.SendAsync(new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = 10
        });

        // Assert
        auditLogs.Items.Count.ShouldBe(5);

        // Verify ordered by audit time descending (most recent first)
        for (int i = 1; i < auditLogs.Items.Count; i++)
        {
            (auditLogs.Items[i - 1].AuditTime >= auditLogs.Items[i].AuditTime).ShouldBeTrue();
        }
    }

    [Test]
    public async Task AuditLogs_CanBeFilteredByDateRange()
    {
        // Arrange
        var beforeTransfer = DateTime.UtcNow;
        
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 750,
            Description = "Date filter test"
        };

        await TestApp.SendAsync(transferCommand);

        var afterTransfer = DateTime.UtcNow;

        // Wait for background service
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Act - Query with matching date range
        var matchingLogsQuery = new GetAuditLogsQuery
        {
            StartDate = beforeTransfer.AddSeconds(-1),
            EndDate = afterTransfer.AddSeconds(10),
            PageNumber = 1,
            PageSize = 10
        };

        var matchingLogs = await TestApp.SendAsync(matchingLogsQuery);

        // Query with non-matching date range (tomorrow)
        var tomorrowQuery = new GetAuditLogsQuery
        {
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            PageNumber = 1,
            PageSize = 10
        };

        var tomorrowLogs = await TestApp.SendAsync(tomorrowQuery);

        // Assert
        matchingLogs.Items.Count.ShouldBeGreaterThan(0);
        tomorrowLogs.Items.Count.ShouldBe(0);
    }

    [Test]
    public async Task AuditLog_ShouldNotBeDuplicatedOnMultipleRuns()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 300,
            Description = "Duplicate test"
        };

        var transaction = await TestApp.SendAsync(transferCommand);

        // First background service run
        await Task.Delay(TimeSpan.FromSeconds(6));

        var firstQuery = new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        var firstAuditLogs = await TestApp.SendAsync(firstQuery);
        var firstCount = firstAuditLogs.Items.Count;

        // Second background service run (simulate another cycle)
        await Task.Delay(TimeSpan.FromSeconds(6));

        var secondAuditLogs = await TestApp.SendAsync(firstQuery);
        var secondCount = secondAuditLogs.Items.Count;

        // Assert - Count should remain the same (no duplicates)
        secondCount.ShouldBe(firstCount);
        
        // Should only have one log for this specific transaction
        var transactionLogs = firstAuditLogs.Items.Where(a => a.TransactionId == transaction.Id).ToList();
        transactionLogs.Count.ShouldBe(1);
    }

    [Test]
    public async Task AuditLog_ShouldNotIncludeFailedTransactions()
    {
        // Arrange - Get initial audit log count
        var initialQuery = new GetAuditLogsQuery { PageNumber = 1, PageSize = 100 };
        var initialLogs = await TestApp.SendAsync(initialQuery);
        var initialCount = initialLogs.Items.Count;

        // Attempt a transfer that will fail
        var failingCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 999999, // Insufficient funds
            Description = "This will fail"
        };

        try
        {
            await TestApp.SendAsync(failingCommand);
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Wait for background service
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Act
        var finalLogs = await TestApp.SendAsync(initialQuery);

        // Assert - No new audit logs should be created
        finalLogs.Items.Count.ShouldBe(initialCount);
    }

    [Test]
    public async Task MultipleTransfers_ShouldAllBeAudited()
    {
        // Arrange
        var transferAmounts = new[] { 100m, 250m, 75m, 500m };
        var createdTransactions = new List<TransactionDto>();

        // Create multiple transfers
        foreach (var amount in transferAmounts)
        {
            var transferCommand = new TransferFundsCommand
            {
                FromAccountNumber = _fromAccount.AccountNumber,
                ToAccountNumber = _toAccount.AccountNumber,
                Amount = amount,
                Description = $"Transfer {amount}"
            };

            var result = await TestApp.SendAsync(transferCommand);
            createdTransactions.Add(result);
        }

        // Wait for background service to process all
        await Task.Delay(TimeSpan.FromSeconds(6));

        // Act
        var auditLogs = await TestApp.SendAsync(new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = 10
        });

        // Assert
        auditLogs.Items.Count.ShouldBeGreaterThanOrEqualTo(4);

        foreach (var transaction in createdTransactions)
        {
            var auditLog = auditLogs.Items.FirstOrDefault(a => a.TransactionId == transaction.Id);
            auditLog.ShouldNotBeNull();
        }
    }
}
