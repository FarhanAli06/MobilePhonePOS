using Microsoft.AspNetCore.Mvc;
using Empire.Web.DTOs.Customer;
using Empire.Web.Authorization;
using Empire.Web.Services;
using Empire.Web.Services.Customer;

namespace Empire.Web.Controllers;

[SessionAuthorizeWithShop]
public class CustomersController : BaseController
{
    private readonly ICustomerApiService _customerApiService;

    public CustomersController(ILogger<CustomersController> logger, ICustomerApiService customerApiService, ITimezoneService timezoneService) : base(logger, timezoneService)
    {
        _customerApiService = customerApiService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                TempData["Error"] = "No shop selected. Please select a shop first.";
                return RedirectToAction("Index", "Shop");
            }

            var customers = await _customerApiService.GetByShopAsync(currentShopId);
            
            // Convert dates to user timezone
            if (customers != null)
            {
                foreach (var customer in customers)
                {
                    customer.CreatedDate = _timezoneService.ConvertToUserTime(customer.CreatedDate);
                    if (customer.ModifiedDate.HasValue)
                    {
                        customer.ModifiedDate = _timezoneService.ConvertToUserTime(customer.ModifiedDate.Value);
                    }
                }
            }            ViewBag.PageTitle = "Customer Management";
            
            return View(customers ?? new List<CustomerDto>());
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error loading customers: {ex.Message}";
            return RedirectToAction("Dashboard", "Home");
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        var currentShopId = GetCurrentShopId();
        if (currentShopId == 0)
        {
            return Json(new { success = false, message = "No shop selected" });
        }        return PartialView("_CreateCustomerModal");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                return Json(new { success = false, message = "Authentication error" });
            }

            request.ShopId = currentShopId;
            var customer = await _customerApiService.CreateAsync(request);
            
            if (customer != null)
            {
                return Json(new { success = true, message = "Customer created successfully", customer = customer });
            }
            else
            {
                return Json(new { success = false, message = "Failed to create customer" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // GET: /Customers/SearchCustomers?term=...
    [HttpGet]
    public async Task<IActionResult> SearchCustomers(string term)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                return Json(new { results = new List<object>() });
            }

            var customers = await _customerApiService.SearchAsync(term, currentShopId);
            
            if (customers == null)
            {
                return Json(new { results = new List<object>() });
            }

            var results = customers.Select(c => new
            {
                id = c.Id,
                text = $"{c.FirstName} {c.LastName} ({c.Phone ?? c.Email ?? "No Contact"})"
            }).ToList();

            return Json(new { results = results });
        }
        catch (Exception)
        {
            return Json(new { results = new List<object>() });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var customer = await _customerApiService.GetByIdAsync(id);
            
            if (customer == null)
            {
                return Json(new { success = false, message = "Customer not found" });
            }

            return PartialView("_EditCustomerModal", customer);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [FromBody] UpdateCustomerRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            // Verify customer exists (API enforces shop ownership via JWT)
            var existingCustomer = await _customerApiService.GetByIdAsync(id);
            if (existingCustomer == null)
            {
                return Json(new { success = false, message = "Customer not found" });
            }
            
            var customer = await _customerApiService.UpdateAsync(id, request);
            
            if (customer != null)
            {
                return Json(new { success = true, message = "Customer updated successfully", customer = customer });
            }
            else
            {
                return Json(new { success = false, message = "Failed to update customer" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            // Verify customer exists (API enforces shop ownership via JWT)
            var customer = await _customerApiService.GetByIdAsync(id);
            if (customer == null)
            {
                return Json(new { success = false, message = "Customer not found" });
            }
            
            var success = await _customerApiService.DeleteAsync(id);
            
            if (success)
            {
                return Json(new { success = true, message = "Customer deleted successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Customer not found or cannot be deleted" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Filter([FromBody] CustomerFilterRequestDto? filter)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            
            // For now, just get all customers by shop
            // You can extend the API to support filtering later
            var customers = await _customerApiService.GetByShopAsync(currentShopId);
            
            return Json(new { success = true, customers = customers ?? new List<CustomerDto>() });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomersForSelection()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var customers = await _customerApiService.GetByShopAsync(currentShopId);
            
            if (customers == null)
            {
                return Json(new { success = true, customers = new List<object>() });
            }

            var customerList = customers.Select(c => new
            {
                id = c.Id,
                name = $"{c.FirstName} {c.LastName}",
                phone = c.Phone,
                email = c.Email
            }).ToList();
            
            return Json(new { success = true, customers = customerList });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerDetails(int id)
    {
        try
        {
            var customer = await _customerApiService.GetByIdAsync(id);
            
            if (customer == null)
            {
                return Json(new { success = false, message = "Customer not found" });
            }
            
            return Json(new { success = true, customer = customer });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}