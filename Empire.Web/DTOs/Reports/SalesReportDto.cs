namespace Empire.Web.DTOs.Reports
{
    /// <summary>
    /// Matches the flat structure of Empire.Application.DTOs.Sale.SalesReportData
    /// returned by the API's SuccessResponse wrapper.
    /// </summary>
    public class SalesReportDto
    {
        public SalesReportSummaryDto Summary { get; set; } = new();
        public List<SalesByDateDto> SalesByDate { get; set; } = new();
        public List<SalesByPaymentMethodDto> SalesByPaymentMethod { get; set; } = new();
        public List<SalesByStatusDto> SalesByStatus { get; set; } = new();
        public List<TopItemDto> TopItems { get; set; } = new();
        public List<SalesReportSaleDto> Sales { get; set; } = new();
    }

    public class SalesReportSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPartial { get; set; }
        public decimal TotalUnpaid { get; set; }
        public decimal AverageSale { get; set; }
    }


    public class SalesByDateDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SalesByPaymentMethodDto
    {
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class TopItemDto
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SalesByStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class SalesReportSaleDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string? CustomerName { get; set; }
        public int ItemCount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public List<SalePaymentEntryDto> Payments { get; set; } = new();
    }

    public class SalePaymentEntryDto
    {
        public int Id { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
    }
}
