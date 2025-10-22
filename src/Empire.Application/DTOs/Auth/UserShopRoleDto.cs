using Empire.Domain.Enums;

namespace Empire.Application.DTOs.Auth;

public class UserShopRoleDto
{
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}

