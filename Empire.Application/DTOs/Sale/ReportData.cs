namespace Empire.Application.DTOs.Sale;

// ── Sales Report ──────────────────────────────────────────────────────────────

public class SalesReportData
{
    public SalesReportSummary Summary { get; set; } = new();
    public List<SalesByDateItem> SalesByDate { get; set; } = new();
    public List<SalesByMethodItem> SalesByPaymentMethod { get; set; } = new();
    public List<SalesByStatusItem> SalesByStatus { get; set; } = new();
    public List<TopSaleItem> TopItems { get; set; } = new();
    public List<SaleDto> Sales { get; set; } = new();
}

public class SalesReportSummary
{
    public decimal TotalRevenue { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPartial { get; set; }
    public decimal TotalUnpaid { get; set; }
    public decimal AverageSale { get; set; }
}

public class SalesByDateItem
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesByMethodItem
{
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class SalesByStatusItem
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class TopSaleItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

// ── Profit Report ─────────────────────────────────────────────────────────────

public class ProfitReportData
{
    public List<ProfitReportRow> Items { get; set; } = new();
    public ProfitReportSummary Summary { get; set; } = new();
}

public class ProfitReportRow
{
    public DateTime SaleDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal Margin { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

public class ProfitReportSummary
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public int SalesCount { get; set; }
    public int ItemsSold { get; set; }
}

// ── Sale Summary ──────────────────────────────────────────────────────────────

public class SaleSummaryData
{
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PaidSales { get; set; }
    public int PartialSales { get; set; }
    public int UnpaidSales { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PartialAmount { get; set; }
    public decimal UnpaidAmount { get; set; }
}
