namespace Empire.Application.DTOs.Auth;

public class UserShopRoleDto
{
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

