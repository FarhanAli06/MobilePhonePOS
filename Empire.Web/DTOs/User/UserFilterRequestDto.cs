using Empire.Web.DTOs.Role;

namespace Empire.Web.DTOs.User;
public class UserFilterRequestDto
{
    public string? SearchTerm { get; set; }
    public int? ShopId { get; set; }
    public RoleDto? Role { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}

