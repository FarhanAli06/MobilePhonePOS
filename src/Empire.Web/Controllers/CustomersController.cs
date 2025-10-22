using Microsoft.AspNetCore.Mvc;
using Empire.Application.DTOs.Customer;
using Empire.Application.Interfaces;
using Empire.Web.Authorization;

namespace Empire.Web.Controllers;

[SessionAuthorizeWithShop]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly IShopService _shopService;

    public CustomersController(ICustomerService customerService, IShopService shopService)
    {
        _customerService = customerService;
        _shopService = shopService;
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

            var customers = await _customerService.GetCustomersByShopAsync(currentShopId);
            ViewBag.CurrentShopId = currentShopId;
            ViewBag.PageTitle = "Customer Management";
            
            return View(customers);
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
        }

        ViewBag.CurrentShopId = currentShopId;
        return PartialView("_CreateCustomerModal");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
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
            var customer = await _customerService.CreateCustomerAsync(request);
            
            return Json(new { success = true, message = "Customer created successfully", customer = customer });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var customer = await _customerService.GetCustomerByIdAsync(id, currentShopId);
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
    public async Task<IActionResult> Edit(int id, [FromBody] UpdateCustomerRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var currentShopId = GetCurrentShopId();
            var customer = await _customerService.UpdateCustomerAsync(id, request);
            
            return Json(new { success = true, message = "Customer updated successfully", customer = customer });
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
            var currentShopId = GetCurrentShopId();
            var success = await _customerService.DeleteCustomerAsync(id, currentShopId);
            
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
    public async Task<IActionResult> Filter([FromBody] CustomerFilterRequest? filter)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            
            // Handle null filter
            if (filter == null)
            {
                filter = new CustomerFilterRequest();
            }
            
            filter.ShopId = currentShopId; // Ensure shop-based filtering
            
            var customers = await _customerService.GetCustomersAsync(filter);
            return Json(new { success = true, customers = customers });
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
            var customers = await _customerService.GetCustomersByShopAsync(currentShopId);
            
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
            var currentShopId = GetCurrentShopId();
            var customer = await _customerService.GetCustomerByIdAsync(id, currentShopId);
            
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

    private int GetCurrentShopId()
    {
        var shopIdString = HttpContext.Session.GetString("CurrentShopId");
        return int.TryParse(shopIdString, out var shopId) ? shopId : 0;
    }
}

