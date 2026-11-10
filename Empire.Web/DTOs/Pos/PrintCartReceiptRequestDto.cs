namespace Empire.Web.DTOs.Pos;

public sealed class PrintCartReceiptRequestDto
{
    public string? CustomerName { get; set; }
    public List<PrintCartReceiptItemDto> Items { get; set; } = [];
    public decimal Total { get; set; }
}

public sealed class PrintCartReceiptItemDto
{
    public string? Name { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
