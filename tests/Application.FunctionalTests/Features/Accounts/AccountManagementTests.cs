using SecureVault.Application.Common.DTOs;
using SecureVault.Application.Common.Exceptions;
using SecureVault.Application.Features.Accounts.Commands;
using SecureVault.Application.Features.Accounts.Queries;
using SecureVault.Domain.Entities;
using AppNotFoundException = SecureVault.Application.Common.Exceptions.NotFoundException;

namespace SecureVault.Application.FunctionalTests.Features.Accounts;

/// <summary>
/// Functional tests for Account Management CRUD operations.
/// Tests: Create, Read, Update (via operations), Delete, and Balance management.
/// </summary>
[TestFixture]
public class AccountManagementTests : TestBase
{
    [Test]
    public async Task CreateAccount_WithValidData_ShouldCreateAccountWithZeroBalance()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            Name = "John Doe",
            Email = "john@example.com"
        };

        // Act
        var result = await TestApp.SendAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("John Doe");
        result.Email.ShouldBe("john@example.com");
        result.Balance.ShouldBe(0);
        result.AccountNumber.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task CreateAccount_WithEmptyName_ShouldThrowValidationException()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            Name = "",
            Email = "john@example.com"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await TestApp.SendAsync(command));
        
        ex.Errors.ShouldContainKey("Name");
    }

    [Test]
    public async Task CreateAccount_WithInvalidEmail_ShouldThrowValidationException()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            Name = "John Doe",
            Email = "invalid-email"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await TestApp.SendAsync(command));
        
        ex.Errors.ShouldContainKey("Email");
    }

    [Test]
    public async Task CreateAccount_WithNameExceedingMaxLength_ShouldThrowValidationException()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            Name = new string('a', 201),
            Email = "john@example.com"
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await TestApp.SendAsync(command));
        
        ex.Errors.ShouldContainKey("Name");
    }

    [Test]
    public async Task GetAllAccounts_WithMultipleAccounts_ShouldReturnAllAccounts()
    {
        // Arrange
        var command1 = new CreateAccountCommand { Name = "Account 1", Email = "account1@example.com" };
        var command2 = new CreateAccountCommand { Name = "Account 2", Email = "account2@example.com" };
        
        await TestApp.SendAsync(command1);
        await TestApp.SendAsync(command2);

        var query = new GetAllAccountsQuery();

        // Act
        var result = await TestApp.SendAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThanOrEqualTo(2);
        result.Any(a => a.Name == "Account 1").ShouldBeTrue();
        result.Any(a => a.Name == "Account 2").ShouldBeTrue();
    }

    [Test]
    public async Task GetAccountByAccountNumber_WithValidAccountNumber_ShouldReturnAccount()
    {
        // Arrange
        var createCommand = new CreateAccountCommand
        {
            Name = "Test Account",
            Email = "test@example.com"
        };
        
        var createdAccount = await TestApp.SendAsync(createCommand);

        var query = new GetAccountByAccountNumberQuery { AccountNumber = createdAccount.AccountNumber };

        // Act
        var result = await TestApp.SendAsync(query);

        // Assert
        result.ShouldNotBeNull();
        result.AccountNumber.ShouldBe(createdAccount.AccountNumber);
        result.Name.ShouldBe("Test Account");
        result.Email.ShouldBe("test@example.com");
    }

    [Test]
    public async Task GetAccountByAccountNumber_WithInvalidAccountNumber_ShouldThrowNotFoundException()
    {
        // Arrange
        var query = new GetAccountByAccountNumberQuery { AccountNumber = "INVALID123" };

        // Act & Assert
        await Should.ThrowAsync<AppNotFoundException>(async () =>
            await TestApp.SendAsync(query));
    }

    [Test]
    public async Task CreditAccount_WithValidData_ShouldIncreaseBalance()
    {
        // Arrange
        var createCommand = new CreateAccountCommand
        {
            Name = "Credit Test",
            Email = "credit@example.com"
        };
        
        var account = await TestApp.SendAsync(createCommand);
        account.Balance.ShouldBe(0);

        var creditCommand = new CreditAccountCommand
        {
            AccountNumber = account.AccountNumber,
            Amount = 1000,
            Description = "Initial credit"
        };

        // Act
        var result = await TestApp.SendAsync(creditCommand);

        // Assert
        result.ShouldNotBeNull();
        result.Balance.ShouldBe(1000);
    }

    [Test]
    public async Task CreditAccount_WithNegativeAmount_ShouldThrowValidationException()
    {
        // Arrange
        var createCommand = new CreateAccountCommand
        {
            Name = "Test",
            Email = "test@example.com"
        };
        
        var account = await TestApp.SendAsync(createCommand);

        var creditCommand = new CreditAccountCommand
        {
            AccountNumber = account.AccountNumber,
            Amount = -100
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await TestApp.SendAsync(creditCommand));
        
        ex.Errors.ShouldContainKey("Amount");
    }

    [Test]
    public async Task CreditAccount_WithZeroAmount_ShouldThrowValidationException()
    {
        // Arrange
        var createCommand = new CreateAccountCommand
        {
            Name = "Test",
            Email = "test@example.com"
        };
        
        var account = await TestApp.SendAsync(createCommand);

        var creditCommand = new CreditAccountCommand
        {
            AccountNumber = account.AccountNumber,
            Amount = 0
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await TestApp.SendAsync(creditCommand));
        
        ex.Errors.ShouldContainKey("Amount");
    }

    [Test]
    public async Task CreditAccount_WithInvalidAccountNumber_ShouldThrowNotFoundException()
    {
        // Arrange
        var creditCommand = new CreditAccountCommand
        {
            AccountNumber = "INVALID123",
            Amount = 100
        };

        // Act & Assert
        await Should.ThrowAsync<AppNotFoundException>(async () =>
            await TestApp.SendAsync(creditCommand));
    }

    [Test]
    public async Task DeleteAccount_WithZeroBalance_ShouldSucceed()
    {
        // Arrange
        var createCommand = new CreateAccountCommand
        {
            Name = "Delete Test",
            Email = "delete@example.com"
        };
        
        var account = await TestApp.SendAsync(createCommand);

        var deleteCommand = new DeleteAccountCommand { AccountNumber = account.AccountNumber };

        // Act
        await TestApp.SendAsync(deleteCommand);

        // Assert - verify account is deleted
        var query = new GetAccountByAccountNumberQuery { AccountNumber = account.AccountNumber };
        await Should.ThrowAsync<AppNotFoundException>(async () =>
            await TestApp.SendAsync(query));
    }

    [Test]
    public async Task DeleteAccount_WithNonZeroBalance_ShouldThrowException()
    {
        // Arrange
        var createCommand = new CreateAccountCommand
        {
            Name = "Delete Test",
            Email = "delete@example.com"
        };
        
        var account = await TestApp.SendAsync(createCommand);

        var creditCommand = new CreditAccountCommand
        {
            AccountNumber = account.AccountNumber,
            Amount = 100
        };
        
        await TestApp.SendAsync(creditCommand);

        var deleteCommand = new DeleteAccountCommand { AccountNumber = account.AccountNumber };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await TestApp.SendAsync(deleteCommand));
    }

    [Test]
    public async Task DeleteAccount_WithInvalidAccountNumber_ShouldThrowNotFoundException()
    {
        // Arrange
        var deleteCommand = new DeleteAccountCommand { AccountNumber = "INVALID123" };

        // Act & Assert
        await Should.ThrowAsync<AppNotFoundException>(async () =>
            await TestApp.SendAsync(deleteCommand));
    }
}
