using SecureVault.Application.Common.DTOs;
using SecureVault.Application.Common.Exceptions;
using SecureVault.Application.Features.Accounts.Commands;
using SecureVault.Application.Features.Accounts.Queries;
using SecureVault.Application.Features.Transactions.Commands;
using SecureVault.Application.Features.Transactions.Queries;
using AppNotFoundException = SecureVault.Application.Common.Exceptions.NotFoundException;

namespace SecureVault.Application.FunctionalTests.Features.Transactions;

/// <summary>
/// Functional tests for Transaction History retrieval.
/// Tests: pagination, filtering, ordering, and transaction listing.
/// </summary>
[TestFixture]
public class TransactionHistoryTests : TestBase
{
    private AccountDto _account = null!;
    private AccountDto _otherAccount = null!;

    [SetUp]
    public async Task TransactionHistoryTestSetUp()
    {
        // Create test accounts
        var createAccountCommand = new CreateAccountCommand
        {
            Name = "Test Account",
            Email = "test@example.com"
        };

        var createOtherCommand = new CreateAccountCommand
        {
            Name = "Other Account",
            Email = "other@example.com"
        };

        _account = await TestApp.SendAsync(createAccountCommand);
        _otherAccount = await TestApp.SendAsync(createOtherCommand);

        // Credit the test account with initial balance
        var creditCommand = new CreditAccountCommand
        {
            AccountNumber = _account.AccountNumber,
            Amount = 5000,
            Description = "Initial balance"
        };

        _account = await TestApp.SendAsync(creditCommand);
    }

    [Test]
    public async Task GetTransactionHistory_WithValidAccountNumber_ShouldReturnTransactions()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _account.AccountNumber,
            ToAccountNumber = _otherAccount.AccountNumber,
            Amount = 100,
            Description = "Test transfer"
        };

        await TestApp.SendAsync(transferCommand);

        var query = new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await TestApp.SendAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBeGreaterThan(0);
        result.Items.Any(t => t.FromAccountNumber == _account.AccountNumber && t.Amount == 100).ShouldBeTrue();
    }

    [Test]
    public async Task GetTransactionHistory_WithMultipleTransactions_ShouldReturnAll()
    {
        // Arrange
        var transfers = new List<TransferFundsCommand>
        {
            new() { FromAccountNumber = _account.AccountNumber, ToAccountNumber = _otherAccount.AccountNumber, Amount = 100, Description = "Transfer 1" },
            new() { FromAccountNumber = _account.AccountNumber, ToAccountNumber = _otherAccount.AccountNumber, Amount = 200, Description = "Transfer 2" },
            new() { FromAccountNumber = _account.AccountNumber, ToAccountNumber = _otherAccount.AccountNumber, Amount = 300, Description = "Transfer 3" }
        };

        // Execute transfers
        foreach (var transfer in transfers)
        {
            await TestApp.SendAsync(transfer);
        }

        var query = new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await TestApp.SendAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(3);
        result.TotalCount.ShouldBe(3);
    }

    [Test]
    public async Task GetTransactionHistory_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange - Create 15 transactions
        for (int i = 0; i < 15; i++)
        {
            var transferCommand = new TransferFundsCommand
            {
                FromAccountNumber = _account.AccountNumber,
                ToAccountNumber = _otherAccount.AccountNumber,
                Amount = 10,
                Description = $"Transfer {i+1}"
            };

            await TestApp.SendAsync(transferCommand);
        }

        // Act - Get first page with 5 items per page
        var page1 = await TestApp.SendAsync(new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
            PageNumber = 1,
            PageSize = 5
        });

        // Get second page
        var page2 = await TestApp.SendAsync(new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
            PageNumber = 2,
            PageSize = 5
        });

        // Get third page
        var page3 = await TestApp.SendAsync(new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
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

        // Verify items are different between pages
        var page1Ids = page1.Items.Select(t => t.Id).ToList();
        var page2Ids = page2.Items.Select(t => t.Id).ToList();
        var page3Ids = page3.Items.Select(t => t.Id).ToList();

        page1Ids.Any(id => page2Ids.Contains(id)).ShouldBeFalse();
        page2Ids.Any(id => page3Ids.Contains(id)).ShouldBeFalse();
    }

    [Test]
    public async Task GetTransactionHistory_WithInvalidAccountNumber_ShouldThrowNotFoundException()
    {
        // Arrange
        var query = new GetTransactionHistoryQuery
        {
            AccountNumber = "INVALID123",
            PageNumber = 1,
            PageSize = 10
        };

        // Act & Assert
        await Should.ThrowAsync<AppNotFoundException>(async () =>
            await TestApp.SendAsync(query));
    }

    [Test]
    public async Task GetTransactionHistory_WithZeroPageNumber_ShouldThrowException()
    {
        // Arrange
        var query = new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
            PageNumber = 0,
            PageSize = 10
        };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await TestApp.SendAsync(query));
    }

    [Test]
    public async Task GetTransactionHistory_WithNegativePageSize_ShouldThrowException()
    {
        // Arrange
        var query = new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
            PageNumber = 1,
            PageSize = -1
        };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await TestApp.SendAsync(query));
    }

    [Test]
    public async Task GetTransactionHistory_ShouldIncludeIncomingTransactions()
    {
        // Arrange - Transfer FROM otherAccount TO testAccount (incoming)
        var creditOtherCommand = new CreditAccountCommand
        {
            AccountNumber = _otherAccount.AccountNumber,
            Amount = 1000,
            Description = "Initial balance for other"
        };

        await TestApp.SendAsync(creditOtherCommand);

        var incomingTransfer = new TransferFundsCommand
        {
            FromAccountNumber = _otherAccount.AccountNumber,
            ToAccountNumber = _account.AccountNumber,
            Amount = 250,
            Description = "Incoming transfer"
        };

        await TestApp.SendAsync(incomingTransfer);

        // Act
        var result = await TestApp.SendAsync(new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
            PageNumber = 1,
            PageSize = 10
        });

        // Assert - Should show the incoming transfer
        result.Items.Any(t => t.ToAccountNumber == _account.AccountNumber && t.Amount == 250).ShouldBeTrue();
    }

    [Test]
    public async Task GetTransactionHistory_ShouldBeOrderedByDateDescending()
    {
        // Arrange - Create multiple transactions with slight delays to ensure ordering
        var transfers = new List<TransferFundsCommand>();
        
        for (int i = 0; i < 5; i++)
        {
            transfers.Add(new TransferFundsCommand
            {
                FromAccountNumber = _account.AccountNumber,
                ToAccountNumber = _otherAccount.AccountNumber,
                Amount = 100,
                Description = $"Transfer {i+1}"
            });
        }

        foreach (var transfer in transfers)
        {
            await TestApp.SendAsync(transfer);
        }

        // Act
        var result = await TestApp.SendAsync(new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
            PageNumber = 1,
            PageSize = 10
        });

        // Assert
        // Note: Result includes 1 credit transaction from setup + 5 transfers = 6 total
        result.Items.Count.ShouldBe(6);

        // Verify transactions are ordered by date descending (most recent first)
        for (int i = 1; i < result.Items.Count; i++)
        {
            (result.Items[i - 1].TransactionDate >= result.Items[i].TransactionDate).ShouldBeTrue();
        }
    }

    [Test]
    public async Task GetTransactionHistory_WithLargePageSize_ShouldReturnAllTransactions()
    {
        // Arrange - Create 10 transactions
        for (int i = 0; i < 10; i++)
        {
            var transferCommand = new TransferFundsCommand
            {
                FromAccountNumber = _account.AccountNumber,
                ToAccountNumber = _otherAccount.AccountNumber,
                Amount = 10,
                Description = $"Transfer {i+1}"
            };

            await TestApp.SendAsync(transferCommand);
        }

        // Act
        var result = await TestApp.SendAsync(new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
            PageNumber = 1,
            PageSize = 100 // Large page size
        });

        // Assert
        // Note: Result includes 1 credit transaction from setup + 10 transfers = 11 total
        result.Items.Count.ShouldBe(11);
        result.TotalCount.ShouldBe(11);
    }

    [Test]
    public async Task GetTransactionHistory_ShouldIncludeTransactionDetails()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _account.AccountNumber,
            ToAccountNumber = _otherAccount.AccountNumber,
            Amount = 250,
            Description = "Detailed transfer"
        };

        var transaction = await TestApp.SendAsync(transferCommand);

        // Act
        var result = await TestApp.SendAsync(new GetTransactionHistoryQuery
        {
            AccountNumber = _account.AccountNumber,
            PageNumber = 1,
            PageSize = 10
        });

        // Assert
        var retrievedTransaction = result.Items.FirstOrDefault(t => t.Id == transaction.Id);
        retrievedTransaction.ShouldNotBeNull();
        retrievedTransaction!.FromAccountNumber.ShouldBe(_account.AccountNumber);
        retrievedTransaction.ToAccountNumber.ShouldBe(_otherAccount.AccountNumber);
        retrievedTransaction.Amount.ShouldBe(250);
        retrievedTransaction.Description.ShouldBe("Detailed transfer");
        retrievedTransaction.TransactionDate.ShouldNotBe(default);
    }

    [Test]
    public async Task GetTransactionHistory_WithEmptyAccountNumber_ShouldThrowValidationException()
    {
        // Arrange
        var query = new GetTransactionHistoryQuery
        {
            AccountNumber = "",
            PageNumber = 1,
            PageSize = 10
        };

        // Act & Assert - The query handler or validator should catch empty account number
        await Should.ThrowAsync<Exception>(async () =>
            await TestApp.SendAsync(query));
    }
}
