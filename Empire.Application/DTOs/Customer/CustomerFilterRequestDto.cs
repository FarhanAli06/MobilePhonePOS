namespace Empire.Application.DTOs.Customer;

public class CustomerFilterRequestDto
{
    public int ShopId { get; set; }
    public string? SearchTerm { get; set; }
}

