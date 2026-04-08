
using Microsoft.AspNetCore.Http.HttpResults;

namespace SecureVault.Web.Endpoints;

public class Transactions : IEndpointGroup
{
   public static void Map(RouteGroupBuilder groupBuilder)
   {
      // groupBuilder.MapGroup("/api/transactions");
      groupBuilder.RequireAuthorization();

      groupBuilder.MapGet(GetTransactionHistory, "history");
   }

   [EndpointSummary("Get Transaction History")]
   [EndpointDescription("Retrieves the transaction history of the authenticated user's account.")]
   public static async Task<Ok<string>> GetTransactionHistory(ISender sender)
   {
      // var id = await sender.Send(command);
      return TypedResults.Ok("User transaction history would be returned here.");
   }
}