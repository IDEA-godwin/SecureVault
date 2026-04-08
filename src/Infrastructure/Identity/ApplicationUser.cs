using Microsoft.AspNetCore.Identity;
using SecureVault.Domain.Entities;

namespace SecureVault.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Account? Account { get; set; }
}
