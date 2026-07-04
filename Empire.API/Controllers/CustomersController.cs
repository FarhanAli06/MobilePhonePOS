using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Customer;

namespace Empire.API.Controllers;

/// <summary>
/// Customer management endpoints
/// </summary>
[Authorize]
public class CustomersController : BaseApiController
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Get all customers for current shop
    /// </summary>
    /// <returns>List of customers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers()
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var customers = await _customerService.GetByShopAsync(shopId);
            return SuccessResponse(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers");
            return ErrorResponse("Error retrieving customers", 500);
        }
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Customer details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer(int id)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);

            if (customer == null)
                return NotFoundResponse("Customer not found");

            return SuccessResponse(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
            return ErrorResponse("Error retrieving customer", 500);
        }
    }

    /// <summary>
    /// Search customers
    /// </summary>
    /// <param name="term">Search term</param>
    /// <returns>Matching customers</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchCustomers([FromQuery] string term)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var customers = await _customerService.SearchCustomersAsync(shopId, term);
            return SuccessResponse(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers with term: {SearchTerm}", term);
            return ErrorResponse("Error searching customers", 500);
        }
    }

    /// <summary>
    /// Create new customer
    /// </summary>
    /// <param name="request">Customer details</param>
    /// <returns>Created customer</returns>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            request.ShopId = shopId;
            var customer = await _customerService.CreateCustomerAsync(request);

            _logger.LogInformation("Customer created: {CustomerId}", customer.Id);

            return StatusCode(201, new
            {
                success = true,
                message = "Customer created successfully",
                data = customer,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return ErrorResponse("Error creating customer", 500);
        }
    }

    /// <summary>
    /// Update customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="request">Updated customer details</param>
    /// <returns>Updated customer</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var customer = await _customerService.UpdateCustomerAsync(id, request);

            if (customer == null)
                return NotFoundResponse("Customer not found");

            _logger.LogInformation("Customer updated: {CustomerId}", id);

            return SuccessResponse(customer, "Customer updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            return ErrorResponse("Error updating customer", 500);
        }
    }

    /// <summary>
    /// Delete customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        try
        {
            await _customerService.DeleteCustomerAsync(id);
            var success = true;

            if (!success)
                return NotFoundResponse("Customer not found");

            _logger.LogInformation("Customer deleted: {CustomerId}", id);

            return SuccessResponse("Customer deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            return ErrorResponse("Error deleting customer", 500);
        }
    }

    /// <summary>
    /// Filter customers
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Filtered customers</returns>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> FilterCustomers([FromBody] CustomerFilterRequestDto filter)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            filter.ShopId = shopId;
            var customers = await _customerService.GetCustomersAsync(shopId);

            return SuccessResponse(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering customers");
            return ErrorResponse("Error filtering customers", 500);
        }
    }
}
