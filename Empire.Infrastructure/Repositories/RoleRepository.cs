using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Empire.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly EmpireDbContext _context;
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(EmpireDbContext context, ILogger<RoleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Roles
                .Include(r => r.UserShopRoles)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by id: {RoleId}", id);
            throw;
        }
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        try
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by name: {RoleName}", name);
            throw;
        }
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        try
        {
            return await _context.Roles
                .OrderBy(r => r.DisplayOrder)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all roles");
            throw;
        }
    }

    public async Task<IEnumerable<Role>> GetActiveRolesAsync()
    {
        try
        {
            return await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.DisplayOrder)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active roles");
            throw;
        }
    }

    public async Task<Role> CreateAsync(Role role)
    {
        try
        {
            role.CreatedAt = DateTime.UtcNow;
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created role: {RoleName} with ID: {RoleId}", role.Name, role.Id);
            return role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {RoleName}", role.Name);
            throw;
        }
    }

    public async Task<Role> UpdateAsync(Role role)
    {
        try
        {
            role.UpdatedAt = DateTime.UtcNow;
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated role: {RoleName} with ID: {RoleId}", role.Name, role.Id);
            return role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {RoleId}", role.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var role = await _context.Roles.FindAsync(id);
            if (role != null)
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted role with ID: {RoleId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role: {RoleId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        try
        {
            return await _context.Roles.AnyAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if role exists: {RoleId}", id);
            throw;
        }
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        try
        {
            var query = _context.Roles.Where(r => r.Name == name);
            
            if (excludeId.HasValue)
            {
                query = query.Where(r => r.Id != excludeId.Value);
            }
            
            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if role name exists: {RoleName}", name);
            throw;
        }
    }
}
