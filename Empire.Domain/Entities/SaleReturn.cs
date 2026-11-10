using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Empire.Domain.Entities;

public class SaleReturn
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    [MaxLength(30)] public string ReturnNumber { get; set; } = string.Empty;
    [MaxLength(50)] public string RefundMethod { get; set; } = "Cash";
    [Column(TypeName = "decimal(18,2)")] public decimal RefundAmount { get; set; }
    [MaxLength(500)] public string Reason { get; set; } = string.Empty;
    [MaxLength(1000)] public string Notes { get; set; } = string.Empty;
    public int ProcessedByUserId { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public virtual Sale Sale { get; set; } = null!;
    public virtual User ProcessedByUser { get; set; } = null!;
    public virtual ICollection<SaleReturnItem> Items { get; set; } = new List<SaleReturnItem>();
}

public class SaleReturnItem
{
    public int Id { get; set; }
    public int SaleReturnId { get; set; }
    public int SaleItemId { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitRefundAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalRefundAmount { get; set; }
    [MaxLength(50)] public string Disposition { get; set; } = "Restock";
    public virtual SaleReturn SaleReturn { get; set; } = null!;
    public virtual SaleItem SaleItem { get; set; } = null!;
}
