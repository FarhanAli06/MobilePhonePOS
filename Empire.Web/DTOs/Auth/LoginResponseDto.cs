using Empire.Web.DTOs.User;
using Empire.Web.DTOs.Role;

namespace Empire.Web.DTOs.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;

    /// <summary>Shop-role associations returned by the API login endpoint.</summary>
    public List<UserShopRoleDto> ShopRoles { get; set; } = new();
}
