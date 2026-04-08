using SecureVault.Application.Common.DTOs;
using SecureVault.Application.Features.Transactions.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace SecureVault.Web.Endpoints;

public class AuditLogEndpoint : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAuditLogs, "");
    }

    [EndpointSummary("Get Audit Logs")]
    [EndpointDescription("Retrieves paginated audit logs for successful transactions. Supports filtering by start and end dates. Dates should be in ISO 8601 format (yyyy-MM-dd).")]
    public static async Task<Ok<PaginatedResult<AuditLogDto>>> GetAuditLogs(
        ISender sender,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAuditLogsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }
}
