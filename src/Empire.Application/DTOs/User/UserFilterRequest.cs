using Empire.Domain.Enums;

namespace Empire.Application.DTOs.User;

public class UserFilterRequest
{
    public string? SearchTerm { get; set; }
    public int? ShopId { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}

