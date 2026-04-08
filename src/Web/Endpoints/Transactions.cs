
using SecureVault.Application.Common.DTOs;
using SecureVault.Application.Features.Transactions.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace SecureVault.Web.Endpoints;

public class Transactions : IEndpointGroup
{
   public static void Map(RouteGroupBuilder groupBuilder)
   {
      groupBuilder.MapGet(GetTransactionHistory, "/history");
   }

   [EndpointSummary("Get Transaction History")]
   [EndpointDescription("Retrieves the paginated transaction history of a specific account, including both incoming and outgoing transfers.")]
   public static async Task<Ok<PaginatedResult<TransactionDto>>> GetTransactionHistory(
      ISender sender,
      [FromQuery] string accountNumber,
      [FromQuery] int pageNumber = 1,
      [FromQuery] int pageSize = 10)
   {
      var query = new GetTransactionHistoryQuery
      {
         AccountNumber = accountNumber,
         PageNumber = pageNumber,
         PageSize = pageSize
      };
      var result = await sender.Send(query);
      return TypedResults.Ok(result);
   }
}