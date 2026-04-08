
namespace SecureVault.Application.Common.DTOs;

public class AccountRequestDto
{
   public string Name { get; set; } = string.Empty;
   public string Email { get; set; } = string.Empty;
   public string Password { get; set; } = string.Empty;
}