using Microsoft.AspNetCore.Mvc.Rendering;
using Empire.Web.DTOs.Common;

namespace Empire.Web.Services.Helper;

/// <summary>
/// Centralized helper service for common operations across the application
/// </summary>
public interface IHelperService
{
    #region Collection Operations
    
    /// <summary>
    /// Counts items matching any of the specified statuses (case-insensitive)
    /// </summary>
    int CountByStatus<T>(IEnumerable<T> items, string[] statuses, Func<T, string> statusSelector);
    
    /// <summary>
    /// Filters items by active status
    /// </summary>
    IEnumerable<T> FilterActive<T>(IEnumerable<T> items, Func<T, bool> isActiveSelector);
    
    /// <summary>
    /// Filters items by status (case-insensitive)
    /// </summary>
    IEnumerable<T> FilterByStatus<T>(IEnumerable<T> items, string status, Func<T, string> statusSelector);
    
    #endregion
    
    #region Calculation Helpers
    
    /// <summary>
    /// Calculates total value from a collection using a value selector
    /// </summary>
    decimal CalculateTotalValue<T>(IEnumerable<T> items, Func<T, decimal> valueSelector);
    
    /// <summary>
    /// Calculates inventory value (Stock * Price)
    /// </summary>
    decimal CalculateInventoryValue<T>(IEnumerable<T> items, Func<T, int> stockSelector, Func<T, decimal> priceSelector);
    
    #endregion
    
    #region Dropdown/SelectList Helpers
    
    /// <summary>
    /// Creates a dropdown list from items with active filtering and sorting
    /// </summary>
    List<DropdownItemDto> CreateDropdownList<T>(
        IEnumerable<T> items,
        Func<T, int> idSelector,
        Func<T, string> nameSelector,
        Func<T, bool>? isActiveSelector = null,
        Func<T, int>? displayOrderSelector = null);
    
    /// <summary>
    /// Creates a SelectList from items
    /// </summary>
    List<SelectListItem> CreateSelectList<T>(
        IEnumerable<T> items,
        Func<T, string> valueSelector,
        Func<T, string> textSelector,
        string? selectedValue = null);
    
    /// <summary>
    /// Creates a SelectList from lookup values
    /// </summary>
    List<SelectListItem> CreateLookupSelectList<T>(
        IEnumerable<T> lookupValues,
        Func<T, string> valueSelector,
        string? selectedValue = null);
    
    #endregion
    
    #region String Helpers
    
    /// <summary>
    /// Formats full name from first and last name
    /// </summary>
    string FormatFullName(string? firstName, string? lastName);
    
    /// <summary>
    /// Formats customer name with optional middle name
    /// </summary>
    string FormatCustomerName(string? firstName, string? middleName, string? lastName);
    
    /// <summary>
    /// Checks if a string matches any of the provided values (case-insensitive)
    /// </summary>
    bool MatchesAny(string? value, params string[] matchValues);
    
    #endregion
    
    #region Anonymous Object Mapping
    
    /// <summary>
    /// Maps collection to anonymous objects with id and name
    /// </summary>
    IEnumerable<object> MapToIdName<T>(IEnumerable<T> items, Func<T, int> idSelector, Func<T, string> nameSelector);
    
    #endregion
}
