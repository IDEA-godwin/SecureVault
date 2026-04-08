using SecureVault.Application.Common;
using SecureVault.Application.Common.DTOs;
using SecureVault.Application.Common.Exceptions;
using SecureVault.Application.Features.Accounts.Commands;
using SecureVault.Application.Features.Accounts.Queries;
using SecureVault.Application.Features.Transactions.Commands;
using SecureVault.Domain.Entities;
using AppNotFoundException = SecureVault.Application.Common.Exceptions.NotFoundException;

namespace SecureVault.Application.FunctionalTests.Features.Transactions;

/// <summary>
/// Functional tests for Transfer Funds Logic.
/// Tests: successful transfers, insufficient funds, negative amounts, atomicity, and constraints.
/// </summary>
[TestFixture]
public class TransferFundsTests : TestBase
{
    private AccountDto _fromAccount = null!;
    private AccountDto _toAccount = null!;

    [SetUp]
    public async Task TransferTestSetUp()
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
            Amount = 1000,
            Description = "Initial balance"
        };

        _fromAccount = await TestApp.SendAsync(creditCommand);
    }

    [Test]
    public async Task TransferFunds_WithSufficientFunds_ShouldSucceed()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 500,
            Description = "Test transfer"
        };

        var initialFromBalance = _fromAccount.Balance;
        var initialToBalance = _toAccount.Balance;

        // Act
        var result = await TestApp.SendAsync(transferCommand);

        // Assert
        result.ShouldNotBeNull();
        result.Amount.ShouldBe(500);
        result.FromAccountNumber.ShouldBe(_fromAccount.AccountNumber);
        result.ToAccountNumber.ShouldBe(_toAccount.AccountNumber);

        // Verify balances were updated correctly
        var fromAccountAfter = await TestApp.SendAsync(new GetAccountByAccountNumberQuery { AccountNumber = _fromAccount.AccountNumber });
        var toAccountAfter = await TestApp.SendAsync(new GetAccountByAccountNumberQuery { AccountNumber = _toAccount.AccountNumber });

        fromAccountAfter.Balance.ShouldBe(initialFromBalance - 500);
        toAccountAfter.Balance.ShouldBe(initialToBalance + 500);
    }

    [Test]
    public async Task TransferFunds_WithInsufficientFunds_ShouldThrowException()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 1500, // More than available balance (1000)
            Description = "Test transfer"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await TestApp.SendAsync(transferCommand));
        
        ex.Message.ShouldContain(ErrorCodes.InsufficientFunds);
    }

    [Test]
    public async Task TransferFunds_WithExactBalance_ShouldSucceed()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 1000, // Exact balance
            Description = "Transfer exact amount"
        };

        // Act
        var result = await TestApp.SendAsync(transferCommand);

        // Assert
        result.ShouldNotBeNull();
        result.Amount.ShouldBe(1000);

        // Verify from account has zero balance
        var fromAccountAfter = await TestApp.SendAsync(new GetAccountByAccountNumberQuery { AccountNumber = _fromAccount.AccountNumber });
        fromAccountAfter.Balance.ShouldBe(0);
    }

    [Test]
    public async Task TransferFunds_WithNegativeAmount_ShouldThrowException()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = -100,
            Description = "Test transfer"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await TestApp.SendAsync(transferCommand));
        
        ex.Errors.ShouldContainKey("Amount");
    }

    [Test]
    public async Task TransferFunds_WithZeroAmount_ShouldThrowException()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 0,
            Description = "Test transfer"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await TestApp.SendAsync(transferCommand));
        
        ex.Errors.ShouldContainKey("Amount");
    }

    [Test]
    public async Task TransferFunds_ToSameAccount_ShouldThrowValidationException()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _fromAccount.AccountNumber, // Same account
            Amount = 100,
            Description = "Test transfer"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await TestApp.SendAsync(transferCommand));
        
        ex.Errors.ShouldContainKey("ToAccountNumber");
    }

    [Test]
    public async Task TransferFunds_WithInvalidFromAccount_ShouldThrowNotFoundException()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = "INVALID123",
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 100,
            Description = "Test transfer"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<AppNotFoundException>(async () =>
            await TestApp.SendAsync(transferCommand));
        
        ex.Message.ShouldContain(ErrorCodes.AccountNotFound);
    }

    [Test]
    public async Task TransferFunds_WithInvalidToAccount_ShouldThrowNotFoundException()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = "INVALID123",
            Amount = 100,
            Description = "Test transfer"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<AppNotFoundException>(async () =>
            await TestApp.SendAsync(transferCommand));
        
        ex.Message.ShouldContain(ErrorCodes.AccountNotFound);
    }

    [Test]
    public async Task TransferFunds_WithEmptyFromAccount_ShouldThrowValidationException()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = "",
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 100,
            Description = "Test transfer"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await TestApp.SendAsync(transferCommand));
        
        ex.Errors.ShouldContainKey("FromAccountNumber");
    }

    [Test]
    public async Task TransferFunds_WithEmptyToAccount_ShouldThrowValidationException()
    {
        // Arrange
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = "",
            Amount = 100,
            Description = "Test transfer"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await TestApp.SendAsync(transferCommand));
        
        ex.Errors.ShouldContainKey("ToAccountNumber");
    }

    [Test]
    public async Task TransferFunds_IsAtomicOnFailure_ShouldRollbackBothAccounts()
    {
        // Arrange - Create a third account to attempt a transfer that will fail
        var createThirdCommand = new CreateAccountCommand
        {
            Name = "Third Account",
            Email = "third@example.com"
        };
        
        var thirdAccount = await TestApp.SendAsync(createThirdCommand);

        var initialFromBalance = _fromAccount.Balance;
        var initialThirdBalance = thirdAccount.Balance;

        // First, perform a successful transfer to change balances
        var successfulTransfer = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = thirdAccount.AccountNumber,
            Amount = 300,
            Description = "Successful transfer"
        };

        await TestApp.SendAsync(successfulTransfer);

        // Verify balances updated
        var afterFirstTransfer = await TestApp.SendAsync(new GetAccountByAccountNumberQuery { AccountNumber = _fromAccount.AccountNumber });
        afterFirstTransfer.Balance.ShouldBe(initialFromBalance - 300);

        // Now attempt a transfer that will fail (insufficient funds)
        var failingTransfer = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = thirdAccount.AccountNumber,
            Amount = 1000, // More than remaining balance
            Description = "This should fail"
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await TestApp.SendAsync(failingTransfer));

        // Verify balances were NOT changed by the failed transfer
        var fromAccountAfterFailed = await TestApp.SendAsync(new GetAccountByAccountNumberQuery { AccountNumber = _fromAccount.AccountNumber });
        var thirdAccountAfterFailed = await TestApp.SendAsync(new GetAccountByAccountNumberQuery { AccountNumber = thirdAccount.AccountNumber });

        // Balance should remain as it was after the successful transfer
        fromAccountAfterFailed.Balance.ShouldBe(initialFromBalance - 300);
        thirdAccountAfterFailed.Balance.ShouldBe(initialThirdBalance + 300);
    }

    [Test]
    public async Task TransferFunds_WithDescription_ShouldIncludeInTransaction()
    {
        // Arrange
        var description = "Monthly rent payment";
        var transferCommand = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 100,
            Description = description
        };

        // Act
        var result = await TestApp.SendAsync(transferCommand);

        // Assert
        result.Description.ShouldBe(description);
    }

    [Test]
    public async Task TransferFunds_MultiplseTransfersFromSameAccount_ShouldUpdateBalanceCorrectly()
    {
        // Arrange - Make multiple transfers from the same account
        var transfer1 = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 200,
            Description = "First transfer"
        };

        var transfer2 = new TransferFundsCommand
        {
            FromAccountNumber = _fromAccount.AccountNumber,
            ToAccountNumber = _toAccount.AccountNumber,
            Amount = 150,
            Description = "Second transfer"
        };

        var initialBalance = _fromAccount.Balance;

        // Act
        await TestApp.SendAsync(transfer1);
        await TestApp.SendAsync(transfer2);

        // Assert
        var fromAccountAfter = await TestApp.SendAsync(new GetAccountByAccountNumberQuery { AccountNumber = _fromAccount.AccountNumber });
        var toAccountAfter = await TestApp.SendAsync(new GetAccountByAccountNumberQuery { AccountNumber = _toAccount.AccountNumber });

        fromAccountAfter.Balance.ShouldBe(initialBalance - 200 - 150);
        toAccountAfter.Balance.ShouldBe(200 + 150);
    }
}
