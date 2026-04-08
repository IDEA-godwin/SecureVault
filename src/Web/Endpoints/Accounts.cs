
using SecureVault.Application.Common.DTOs;
using SecureVault.Application.Features.Accounts.Commands;
using SecureVault.Application.Features.Accounts.Queries;
using SecureVault.Application.Features.Transactions.Commands;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace SecureVault.Web.Endpoints;

public class Accounts : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {

        groupBuilder.MapGet(GetAllAccounts, "");
        groupBuilder.MapPost(CreateAccount, "");
        groupBuilder.MapGet(GetAccountByAccountNumber, "{accountNumber}");
        groupBuilder.MapPatch(CreditAccount, "credit");
        groupBuilder.MapDelete(DeleteAccount, "{accountNumber}");

        groupBuilder.MapPost(Transfer, "transfer");
    }

    [EndpointSummary("Get All Accounts")]
    [EndpointDescription("Retrieves a list of all accounts.")]
    public static async Task<Ok<List<AccountDto>>> GetAllAccounts(ISender sender)
    {
        var query = new GetAllAccountsQuery();
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create Account")]
    [EndpointDescription("Creates a new account with Name and Email. Account starts with zero balance.")]
    public static async Task<Created<AccountDto>> CreateAccount(ISender sender, CreateAccountCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Created($"/api/accounts/{result.AccountNumber}", result);
    }

    [EndpointSummary("Get Account")]
    [EndpointDescription("Retrieves account information by account number.")]
    public static async Task<Ok<AccountDto>> GetAccountByAccountNumber(ISender sender, string accountNumber)
    {
        var query = new GetAccountByAccountNumberQuery { AccountNumber = accountNumber };
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    [EndpointSummary("Credit Account")]
    [EndpointDescription("Credits (adds funds to) an account. Creates a credit transaction. Requires amount parameter.")]
    public static async Task<Ok<AccountDto>> CreditAccount(ISender sender, CreditAccountCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Ok(result);
    }

    [EndpointSummary("Delete Account")]
    [EndpointDescription("Deletes an account. Account must have zero balance.")]
    public static async Task<NoContent> DeleteAccount(ISender sender, string accountNumber)
    {
        var command = new DeleteAccountCommand { AccountNumber = accountNumber };
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Transfer Funds")]
    [EndpointDescription("Transfers funds between two accounts. Validates sufficient funds and prevents negative transfers. Transactions are atomic.")]
    public static async Task<Ok<TransactionDto>> Transfer(ISender sender, TransferFundsCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Ok(result);
    }
}