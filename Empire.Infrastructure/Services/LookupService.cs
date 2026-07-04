using Empire.Application.DTOs.Lookup;
using Empire.Application.Interfaces;
using Empire.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Empire.Infrastructure.Services;
public class LookupService : ILookupService
{
    private readonly EmpireDbContext _context;
    private readonly ILogger<LookupService> _logger;
    public LookupService(EmpireDbContext context, ILogger<LookupService> logger)
    {
        _context = context;
        _logger = logger;
    }
    public async Task<List<LookupValueDto>> GetAllAsync(int shopId)
    {
        try
        {
            return await _context.LookupValues
                .Include(lv => lv.Styling)
                .Where(lv => lv.IsActive && (lv.ShopId == shopId || lv.ShopId == 0))
                .OrderBy(lv => lv.Category)
                .ThenBy(lv => lv.DisplayOrder)
                .Select(lv => new LookupValueDto
                {
                    Id = lv.Id,
                    Category = lv.Category,
                    Value = lv.Value,
                    Description = lv.Description,
                    IsActive = lv.IsActive,
                    DisplayOrder = lv.DisplayOrder,
                    ColorCode = lv.ColorCode,
                    Icon = lv.Styling != null ? lv.Styling.Icon : "label",
                    Color = lv.Styling != null ? lv.Styling.Color : lv.ColorCode
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all lookup values for shop {ShopId}", shopId);
            throw;
        }
    }
    public async Task<List<LookupValueDto>> GetByCategoryAsync(string category, int shopId)
    {
        try
        {
            return await _context.LookupValues
                .Include(lv => lv.Styling)
                .Where(lv => lv.Category == category && lv.IsActive
                             && (lv.ShopId == shopId || lv.ShopId == 0))
                .OrderBy(lv => lv.DisplayOrder)
                .Select(lv => new LookupValueDto
                {
                    Id = lv.Id,
                    Category = lv.Category,
                    Value = lv.Value,
                    Description = lv.Description,
                    IsActive = lv.IsActive,
                    DisplayOrder = lv.DisplayOrder,
                    ColorCode = lv.ColorCode,
                    Icon = lv.Styling != null ? lv.Styling.Icon : "label",
                    Color = lv.Styling != null ? lv.Styling.Color : lv.ColorCode
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup values for category {Category}, shop {ShopId}", category, shopId);
            throw;
        }
    }
    public async Task<LookupValueDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.LookupValues
                .Include(lv => lv.Styling)
                .Where(lv => lv.Id == id)
                .Select(lv => new LookupValueDto
                {
                    Id = lv.Id,
                    Category = lv.Category,
                    Value = lv.Value,
                    Description = lv.Description,
                    IsActive = lv.IsActive,
                    DisplayOrder = lv.DisplayOrder,
                    ColorCode = lv.ColorCode,
                    Icon = lv.Styling != null ? lv.Styling.Icon : "label",
                    Color = lv.Styling != null ? lv.Styling.Color : lv.ColorCode
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup value by id: {Id}", id);
            throw;
        }
    }
    public async Task<List<string>> GetCategoriesAsync(int shopId)
    {
        try
        {
            return await _context.LookupValues
                .Where(lv => lv.IsActive && (lv.ShopId == shopId || lv.ShopId == 0))
                .Select(lv => lv.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup categories for shop {ShopId}", shopId);
            throw;
        }
    }
}
