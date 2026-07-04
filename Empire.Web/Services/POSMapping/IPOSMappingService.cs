using Empire.Web.DTOs.Pos;
using Empire.Web.DTOs.Sale;

namespace Empire.Web.Services.POSMapping;

/// <summary>
/// Service for mapping POS-related DTOs to response objects
/// </summary>
public interface IPOSMappingService
{
    /// <summary>
    /// Maps POS product search results to anonymous objects
    /// </summary>
    IEnumerable<object> MapProductSearchResults(IEnumerable<POSProductDto> products);
    
    /// <summary>
    /// Maps customer search results to Select2-compatible format
    /// </summary>
    IEnumerable<object> MapCustomerSearchResults(IEnumerable<POSCustomerDto> customers);
    
    /// <summary>
    /// Maps repair search results to anonymous objects
    /// </summary>
    IEnumerable<object> MapRepairSearchResults(IEnumerable<POSRepairDto> repairs);
    
    /// <summary>
    /// Maps CreateSaleRequestDto to POSCreateSaleRequestDto
    /// </summary>
    POSCreateSaleRequestDto MapToPosSaleRequest(CreateSaleRequestDto request, int shopId, int userId);
}
