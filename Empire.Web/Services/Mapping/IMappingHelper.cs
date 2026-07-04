using Empire.Web.DTOs.Inventory;

namespace Empire.Web.Services.Mapping;

/// <summary>
/// Helper service for common object mappings
/// Centralizes anonymous object creation and DTO mappings
/// </summary>
public interface IMappingHelper
{
    /// <summary>
    /// Maps inventory item DTOs to anonymous objects for JSON response
    /// </summary>
    object MapInventoryItemToAnonymous(InventoryItemDto item);
    
    /// <summary>
    /// Maps a collection of inventory items to anonymous objects
    /// </summary>
    IEnumerable<object> MapInventoryItemsToAnonymous(IEnumerable<InventoryItemDto> items);
    
    /// <summary>
    /// Maps simple ID/Name pairs to anonymous objects
    /// </summary>
    object MapToIdName(int id, string name);
    
    /// <summary>
    /// Maps POS product search results to anonymous objects
    /// </summary>
    object MapPOSProductToAnonymous(dynamic product);
    
    /// <summary>
    /// Maps customer search results to anonymous objects
    /// </summary>
    object MapCustomerToAnonymous(dynamic customer);
    
    /// <summary>
    /// Maps repair search results to anonymous objects
    /// </summary>
    object MapRepairToAnonymous(dynamic repair);
}
