namespace Empire.Application.DTOs.Customer;

public class CustomerFilterRequest
{
    public int ShopId { get; set; }
    public string? SearchTerm { get; set; }
}

