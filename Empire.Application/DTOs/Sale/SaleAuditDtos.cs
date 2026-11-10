namespace Empire.Application.DTOs.Sale;

public class SaleHistoryDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
}

public class ProcessSaleReturnRequest
{
    public int ShopId { get; set; }
    public string RefundMethod { get; set; } = "Cash";
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int UserId { get; set; }
    public List<ProcessSaleReturnItemRequest> Items { get; set; } = new();
}

public class ProcessSaleReturnItemRequest
{
    public int SaleItemId { get; set; }
    public int Quantity { get; set; }
    public decimal RefundAmount { get; set; }
    public string Disposition { get; set; } = "Restock";
}

public class SaleReturnResultDto
{
    public int Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

public class SaleReturnHistoryDto
{
    public int Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public string RefundMethod { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ProcessedBy { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public List<SaleReturnItemHistoryDto> Items { get; set; } = new();
}

public class SaleReturnItemHistoryDto
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal RefundAmount { get; set; }
    public string Disposition { get; set; } = string.Empty;
}
