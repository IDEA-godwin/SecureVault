using Microsoft.AspNetCore.Http.HttpResults;

namespace SecureVault.Web.Endpoints;

public class Users : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        // Users endpoint group is now empty, account management is handled via Accounts endpoint
        // This class is kept for future user-related endpoints if needed
    }
}
