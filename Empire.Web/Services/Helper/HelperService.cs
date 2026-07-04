using Microsoft.AspNetCore.Mvc.Rendering;
using Empire.Web.DTOs.Common;

namespace Empire.Web.Services.Helper;

/// <summary>
/// Centralized helper service implementation for common operations
/// </summary>
public class HelperService : IHelperService
{
    #region Collection Operations
    
    /// <summary>
    /// Counts items matching any of the specified statuses (case-insensitive)
    /// </summary>
    public int CountByStatus<T>(IEnumerable<T> items, string[] statuses, Func<T, string> statusSelector)
    {
        if (items == null || !items.Any() || statuses == null || !statuses.Any())
            return 0;
            
        return items.Count(item =>
        {
            var itemStatus = statusSelector(item);
            return statuses.Any(status =>
                status.Equals(itemStatus, StringComparison.OrdinalIgnoreCase));
        });
    }
    
    /// <summary>
    /// Filters items by active status
    /// </summary>
    public IEnumerable<T> FilterActive<T>(IEnumerable<T> items, Func<T, bool> isActiveSelector)
    {
        if (items == null || !items.Any())
            return Enumerable.Empty<T>();
            
        return items.Where(isActiveSelector);
    }
    
    /// <summary>
    /// Filters items by status (case-insensitive)
    /// </summary>
    public IEnumerable<T> FilterByStatus<T>(IEnumerable<T> items, string status, Func<T, string> statusSelector)
    {
        if (items == null || !items.Any() || string.IsNullOrWhiteSpace(status))
            return items ?? Enumerable.Empty<T>();
            
        return items.Where(item =>
            statusSelector(item).Equals(status, StringComparison.OrdinalIgnoreCase));
    }
    
    #endregion
    
    #region Calculation Helpers
    
    /// <summary>
    /// Calculates total value from a collection using a value selector
    /// </summary>
    public decimal CalculateTotalValue<T>(IEnumerable<T> items, Func<T, decimal> valueSelector)
    {
        if (items == null || !items.Any())
            return 0;
            
        return items.Sum(valueSelector);
    }
    
    /// <summary>
    /// Calculates inventory value (Stock * Price)
    /// </summary>
    public decimal CalculateInventoryValue<T>(IEnumerable<T> items, Func<T, int> stockSelector, Func<T, decimal> priceSelector)
    {
        if (items == null || !items.Any())
            return 0;
            
        return items.Sum(item => stockSelector(item) * priceSelector(item));
    }
    
    #endregion
    
    #region Dropdown/SelectList Helpers
    
    /// <summary>
    /// Creates a dropdown list from items with active filtering and sorting
    /// </summary>
    public List<DropdownItemDto> CreateDropdownList<T>(
        IEnumerable<T> items,
        Func<T, int> idSelector,
        Func<T, string> nameSelector,
        Func<T, bool>? isActiveSelector = null,
        Func<T, int>? displayOrderSelector = null)
    {
        if (items == null || !items.Any())
            return new List<DropdownItemDto>();
        
        // Filter by active if selector provided
        var filteredItems = isActiveSelector != null
            ? items.Where(isActiveSelector)
            : items;
        
        // Sort by display order if provided, then by name
        var sortedItems = displayOrderSelector != null
            ? filteredItems.OrderBy(displayOrderSelector).ThenBy(nameSelector)
            : filteredItems.OrderBy(nameSelector);
        
        return sortedItems
            .Select(item => new DropdownItemDto
            {
                Id = idSelector(item),
                Name = nameSelector(item)
            })
            .ToList();
    }
    
    /// <summary>
    /// Creates a SelectList from items
    /// </summary>
    public List<SelectListItem> CreateSelectList<T>(
        IEnumerable<T> items,
        Func<T, string> valueSelector,
        Func<T, string> textSelector,
        string? selectedValue = null)
    {
        if (items == null || !items.Any())
            return new List<SelectListItem>();
        
        return items.Select(item => new SelectListItem
        {
            Value = valueSelector(item),
            Text = textSelector(item),
            Selected = selectedValue != null && valueSelector(item) == selectedValue
        }).ToList();
    }
    
    /// <summary>
    /// Creates a SelectList from lookup values
    /// </summary>
    public List<SelectListItem> CreateLookupSelectList<T>(
        IEnumerable<T> lookupValues,
        Func<T, string> valueSelector,
        string? selectedValue = null)
    {
        if (lookupValues == null || !lookupValues.Any())
            return new List<SelectListItem>();
        
        return lookupValues.Select(lv => new SelectListItem
        {
            Value = valueSelector(lv),
            Text = valueSelector(lv),
            Selected = selectedValue != null && valueSelector(lv) == selectedValue
        }).ToList();
    }
    
    #endregion
    
    #region String Helpers
    
    /// <summary>
    /// Formats full name from first and last name
    /// </summary>
    public string FormatFullName(string? firstName, string? lastName)
    {
        var parts = new[] { firstName?.Trim(), lastName?.Trim() }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        
        return string.Join(" ", parts);
    }
    
    /// <summary>
    /// Formats customer name with optional middle name
    /// </summary>
    public string FormatCustomerName(string? firstName, string? middleName, string? lastName)
    {
        var parts = new[] { firstName?.Trim(), middleName?.Trim(), lastName?.Trim() }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        
        return string.Join(" ", parts);
    }
    
    /// <summary>
    /// Checks if a string matches any of the provided values (case-insensitive)
    /// </summary>
    public bool MatchesAny(string? value, params string[] matchValues)
    {
        if (string.IsNullOrWhiteSpace(value) || matchValues == null || !matchValues.Any())
            return false;
        
        return matchValues.Any(match =>
            match.Equals(value, StringComparison.OrdinalIgnoreCase));
    }
    
    #endregion
    
    #region Anonymous Object Mapping
    
    /// <summary>
    /// Maps collection to anonymous objects with id and name
    /// </summary>
    public IEnumerable<object> MapToIdName<T>(IEnumerable<T> items, Func<T, int> idSelector, Func<T, string> nameSelector)
    {
        if (items == null || !items.Any())
            return Enumerable.Empty<object>();
        
        return items.Select(item => new { Id = idSelector(item), Name = nameSelector(item) });
    }
    
    #endregion
}
