using Empire.Application.DTOs.Customer;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;

namespace Empire.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<IEnumerable<CustomerDto>> GetCustomersAsync(CustomerFilterRequest filter)
    {
        var customers = await _customerRepository.GetCustomersByShopAsync(filter.ShopId, filter.SearchTerm);
        return customers.Select(MapToCustomerDto);
    }

    public async Task<IEnumerable<CustomerDto>> GetCustomersByShopAsync(int shopId)
    {
        var customers = await _customerRepository.GetCustomersByShopAsync(shopId, null);
        return customers.Select(MapToCustomerDto);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int customerId, int shopId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null || customer.ShopId != shopId)
            return null;

        return MapToCustomerDto(customer);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            ShopId = request.ShopId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Email = request.Email ?? string.Empty,
            Address = request.Address ?? string.Empty,
            City = request.City ?? string.Empty,
            State = request.State ?? string.Empty,
            ZipCode = request.ZipCode ?? string.Empty,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();
        return MapToCustomerDto(customer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null)
            throw new ArgumentException("Customer not found");

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.Phone = request.Phone;
        customer.Email = request.Email ?? string.Empty;
        customer.Address = request.Address ?? string.Empty;
        customer.City = request.City ?? string.Empty;
        customer.State = request.State ?? string.Empty;
        customer.ZipCode = request.ZipCode ?? string.Empty;
        customer.ModifiedDate = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer);
        await _customerRepository.SaveChangesAsync();
        return MapToCustomerDto(customer);
    }

    public async Task<bool> DeleteCustomerAsync(int customerId, int shopId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null || customer.ShopId != shopId)
            return false;

        await _customerRepository.DeleteAsync(customer);
        await _customerRepository.SaveChangesAsync();
        return true;
    }

    private static CustomerDto MapToCustomerDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Phone = customer.Phone,
            Email = string.IsNullOrEmpty(customer.Email) ? null : customer.Email,
            Address = string.IsNullOrEmpty(customer.Address) ? null : customer.Address,
            City = string.IsNullOrEmpty(customer.City) ? null : customer.City,
            State = string.IsNullOrEmpty(customer.State) ? null : customer.State,
            ZipCode = string.IsNullOrEmpty(customer.ZipCode) ? null : customer.ZipCode,
            CreatedDate = customer.CreatedDate,
            ModifiedDate = customer.ModifiedDate
        };
    }
}

