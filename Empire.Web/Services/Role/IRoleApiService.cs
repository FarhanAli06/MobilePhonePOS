using Empire.Web.DTOs.Role;

namespace Empire.Web.Services.Role;

public interface IRoleApiService
{
    Task<List<RoleDto>> GetAllAsync();
    Task<List<RoleDto>> GetActiveAsync();
    Task<RoleDto?> GetByIdAsync(int id);
}
