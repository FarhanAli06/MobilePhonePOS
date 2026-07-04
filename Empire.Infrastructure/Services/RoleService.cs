using Empire.Application.DTOs.Role;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Empire.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<RoleService> _logger;

    public RoleService(IRoleRepository roleRepository, ILogger<RoleService> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        return role != null ? MapToDto(role) : null;
    }

    public async Task<RoleDto?> GetByNameAsync(string name)
    {
        var role = await _roleRepository.GetByNameAsync(name);
        return role != null ? MapToDto(role) : null;
    }

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return roles.Select(MapToDto);
    }

    public async Task<IEnumerable<RoleDto>> GetActiveRolesAsync()
    {
        var roles = await _roleRepository.GetActiveRolesAsync();
        return roles.Select(MapToDto);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request)
    {
        // Check if role name already exists
        if (await _roleRepository.NameExistsAsync(request.Name))
        {
            throw new ArgumentException($"Role with name '{request.Name}' already exists");
        }

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        var createdRole = await _roleRepository.CreateAsync(role);
        _logger.LogInformation("Created role: {RoleName} with ID: {RoleId}", createdRole.Name, createdRole.Id);

        return MapToDto(createdRole);
    }

    public async Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            throw new ArgumentException($"Role with ID {id} not found");
        }

        // Check if new name already exists for another role
        if (await _roleRepository.NameExistsAsync(request.Name, id))
        {
            throw new ArgumentException($"Role with name '{request.Name}' already exists");
        }

        role.Name = request.Name;
        role.Description = request.Description;
        role.IsActive = request.IsActive;
        role.DisplayOrder = request.DisplayOrder;
        role.UpdatedAt = DateTime.UtcNow;

        var updatedRole = await _roleRepository.UpdateAsync(role);
        _logger.LogInformation("Updated role: {RoleName} with ID: {RoleId}", updatedRole.Name, updatedRole.Id);

        return MapToDto(updatedRole);
    }

    public async Task DeleteAsync(int id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            throw new ArgumentException($"Role with ID {id} not found");
        }

        await _roleRepository.DeleteAsync(id);
        _logger.LogInformation("Deleted role with ID: {RoleId}", id);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        return await _roleRepository.NameExistsAsync(name, excludeId);
    }

    private static RoleDto MapToDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            DisplayOrder = role.DisplayOrder,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}
