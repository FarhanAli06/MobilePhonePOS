using Empire.Web.DTOs.Pos;
using Empire.Web.DTOs.Sale;
using Empire.Web.DTOs.SaleItem;

namespace Empire.Web.Services.POSMapping;

/// <summary>
/// Implementation of POS mapping service
/// </summary>
public class POSMappingService : IPOSMappingService
{
    /// <summary>
    /// Maps POS product search results to anonymous objects
    /// </summary>
    public IEnumerable<object> MapProductSearchResults(IEnumerable<POSProductDto> products)
    {
        return products.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            brand = p.Brand,
            category = p.Category,
            sku = p.Sku,
            price = p.Price,
            wholesalePrice = p.WholesalePrice,
            stock = p.Stock,
            type = p.Type,
            description = p.Description
        });
    }
    
    /// <summary>
    /// Maps customer search results to Select2-compatible format
    /// </summary>
    public IEnumerable<object> MapCustomerSearchResults(IEnumerable<POSCustomerDto> customers)
    {
        return customers.Select(c => new
        {
            id = c.Id,
            text = $"{c.Name} - {c.Phone}",
            name = c.Name,
            phone = c.Phone,
            email = c.Email
        });
    }
    
    /// <summary>
    /// Maps repair search results to anonymous objects
    /// </summary>
    public IEnumerable<object> MapRepairSearchResults(IEnumerable<POSRepairDto> repairs)
    {
        return repairs.Select(r => new
        {
            id = r.Id,
            repairNumber = r.RepairNumber,
            customerName = r.CustomerName,
            description = r.Description,
            cost = r.Cost,
            status = r.Status
        });
    }
    
    /// <summary>
    /// Maps CreateSaleRequestDto to POSCreateSaleRequestDto
    /// </summary>
    public POSCreateSaleRequestDto MapToPosSaleRequest(CreateSaleRequestDto request, int shopId, int userId)
    {
        return new POSCreateSaleRequestDto
        {
            ShopId = shopId,
            CustomerId = request.CustomerId,
            Items = request.Items.Select(i => new POSSaleItemRequestDto
            {
                ItemType        = i.ItemType,
                ItemReferenceId = i.ItemReferenceId,
                InventoryItemId = i.InventoryItemId,
                ItemName        = i.ItemName,
                Description     = i.Description,
                Quantity        = i.Quantity,
                UnitPrice       = i.UnitPrice,
                TotalPrice      = i.TotalPrice,
                Cost            = i.Cost ?? i.CostPrice,  // prefer Cost, fall back to CostPrice
                DiscountAmount  = i.DiscountAmount,
                IsCustomItem    = i.IsCustomItem,
                IsTaxable       = i.IsTaxable,
                Note            = i.Note,
                RepairId        = i.RepairId
            }).ToList(),
            Payments = request.Payments.Select(p => new POSPaymentRequestDto
            {
                PaymentMethod = p.PaymentMethod,
                Amount = p.Amount,
                ReferenceNumber = p.ReferenceNumber
            }).ToList(),
            SubTotal = request.SubTotal,
            TaxAmount = request.TaxAmount,
            DiscountAmount = request.DiscountAmount,
            TotalAmount = request.TotalAmount,
            PaymentStatus = request.PaymentStatus,
            CreatedByUserId = userId
        };
    }
}
