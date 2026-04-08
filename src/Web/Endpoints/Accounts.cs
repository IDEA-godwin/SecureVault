
using Microsoft.AspNetCore.Http.HttpResults;

namespace SecureVault.Web.Endpoints;

public class Accounts : IEndpointGroup
{
   public static void Map(RouteGroupBuilder groupBuilder)
   {
      // groupBuilder.MapGroup("/api/accounts");
      groupBuilder.RequireAuthorization();

      groupBuilder.MapGet(GetUserAccountInfo);
      groupBuilder.MapGet(GetAccountBalance, "balance");
      groupBuilder.MapPost(Transfer, "transfer/{account}");
   }

   [EndpointSummary("Get User Account Info")]
   [EndpointDescription("Retrieves information about the authenticated user's account information.")]
   public static async Task<Ok<string>> GetUserAccountInfo(ISender sender)
   {
      // var id = await sender.Send(command);
      return TypedResults.Ok("User account information would be returned here.");
   }

   [EndpointSummary("Get User Account Balance")]
   [EndpointDescription("Retrieves the balance of the authenticated user's account.")]
   public static async Task<Ok<string>> GetAccountBalance(ISender sender)
   {
      // var id = await sender.Send(command);
      return TypedResults.Ok("User account balance would be returned here.");
   }


   [EndpointSummary("Transfer Funds")]
   [EndpointDescription("Transfers funds between the authenticated user's accounts.")]
   public static async Task<Ok<string>> Transfer(ISender sender, string account)
   {
      // var id = await sender.Send(command);
      return TypedResults.Ok("User performs transfer action here.");
   }
}