namespace Empire.Web.DTOs.Reports
{
    public class ProfitReportDto
    {
        public List<ProfitReportItemDto> Items { get; set; } = new();
        public ProfitReportSummaryDto Summary { get; set; } = new();
    }

    public class ProfitReportItemDto
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

    public class ProfitReportSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public int SalesCount { get; set; }
        public int ItemsSold { get; set; }
    }
}
