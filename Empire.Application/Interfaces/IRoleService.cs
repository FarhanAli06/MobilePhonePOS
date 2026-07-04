using Empire.Application.DTOs.Role;

namespace Empire.Application.Interfaces;

public interface IRoleService
{
    Task<RoleDto?> GetByIdAsync(int id);
    Task<RoleDto?> GetByNameAsync(string name);
    Task<IEnumerable<RoleDto>> GetAllAsync();
    Task<IEnumerable<RoleDto>> GetActiveRolesAsync();
    Task<RoleDto> CreateAsync(CreateRoleRequest request);
    Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request);
    Task DeleteAsync(int id);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
}
