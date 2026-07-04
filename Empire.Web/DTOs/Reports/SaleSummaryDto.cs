namespace Empire.Web.DTOs.Reports
{
    public class SaleSummaryDto
    {
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PaidSales { get; set; }
        public int PendingSales { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
    }
}
