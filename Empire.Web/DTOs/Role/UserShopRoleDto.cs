namespace Empire.Web.DTOs.Role
{
    public class UserShopRoleDto
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
