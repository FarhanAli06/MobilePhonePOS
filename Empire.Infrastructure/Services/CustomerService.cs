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

    public async Task<IEnumerable<CustomerDto>> GetAsync(CustomerFilterRequestDto filter)
    {
        var customers = await _customerRepository.GetByShopAsync(filter.ShopId, filter.SearchTerm);
        return customers.Select(MapToCustomerDto);
    }

    public async Task<IEnumerable<CustomerDto>> GetByShopAsync(int shopId)
    {
        var customers = await _customerRepository.GetByShopAsync(shopId, null);
        return customers.Select(MapToCustomerDto);
    }

    public async Task<CustomerDto?> GetByIdAsync(int customerId, int shopId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null || customer.ShopId != shopId)
            return null;

        return MapToCustomerDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
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
            ZipCode = request.ZipCode ?? string.Empty
            // CreatedDate and ModifiedDate are set automatically by DbContext.SaveChangesAsync
        };

        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();
        return MapToCustomerDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(int customerId, UpdateCustomerRequest request)
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
        // ModifiedDate is set automatically by DbContext.SaveChangesAsync

        await _customerRepository.UpdateAsync(customer);
        await _customerRepository.SaveChangesAsync();
        return MapToCustomerDto(customer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request)
    {
        return await UpdateAsync(customerId, request);
    }

    public async Task<bool> DeleteAsync(int customerId, int shopId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null || customer.ShopId != shopId)
            return false;

        await _customerRepository.DeleteAsync(customer);
        await _customerRepository.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CustomerDto>> SearchAsync(int shopId, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Enumerable.Empty<CustomerDto>();
        }

        var customers = await _customerRepository.SearchAsync(shopId, term);
        return customers.Select(MapToCustomerDto);
    }

    public async Task<IEnumerable<CustomerDto>> SearchCustomersAsync(int shopId, string searchTerm)
    {
        return await SearchAsync(shopId, searchTerm);
    }

    public async Task<IEnumerable<CustomerDto>> GetCustomersAsync(int shopId)
    {
        return await GetByShopAsync(shopId);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        return customer == null ? null : MapToCustomerDto(customer);
    }

    public async Task DeleteCustomerAsync(int customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer != null)
        {
            await _customerRepository.DeleteAsync(customer);
            await _customerRepository.SaveChangesAsync();
        }
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request)
    {
        return await CreateAsync(request);
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

