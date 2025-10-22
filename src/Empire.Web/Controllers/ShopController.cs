using Microsoft.AspNetCore.Mvc;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Shop;
using Empire.Domain.Enums;

namespace Empire.Web.Controllers;

public class ShopController : Controller
{
    private readonly IShopService _shopService;
    private readonly IUserService _userService;

    public ShopController(IShopService shopService, IUserService userService)
    {
        _shopService = shopService;
        _userService = userService;
    }

    public async Task<IActionResult> Index()
    {
        if (!IsAuthenticated())
        {
            return RedirectToAction("Login", "Home");
        }
        
        var userId = GetCurrentUserId();
        var shops = await _shopService.GetShopsByUserAsync(userId);
        return View(shops);
    }

    public IActionResult Create()
    {
        if (!IsAuthenticated())
        {
            return RedirectToAction("Login", "Home");
        }
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShopRequest request)
    {
        if (!IsAuthenticated())
        {
            return RedirectToAction("Login", "Home");
        }
        
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            var userId = GetCurrentUserId();
            
            // Create shop first
            var shop = await _shopService.CreateShopAsync(request, userId);
            
            // Then assign the creator as Manager of the shop
            await _userService.AssignUserToShopAsync(userId, shop.Id, UserRole.Manager);
            
            TempData["SuccessMessage"] = "Shop created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error creating shop: {ex.Message}");
            return View(request);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var shop = await _shopService.GetShopByIdAsync(id);
            if (shop == null)
            {
                return NotFound();
            }

            var updateRequest = new UpdateShopRequest
            {
                Name = shop.Name,
                Address = shop.Address,
                City = shop.City,
                State = shop.State,
                ZipCode = shop.ZipCode,
                Phone = shop.Phone,
                Email = shop.Email
            };

            return View(updateRequest);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error loading shop: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateShopRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            var userId = GetCurrentUserId();
            await _shopService.UpdateShopAsync(id, request, userId);
            
            TempData["SuccessMessage"] = "Shop updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error updating shop: {ex.Message}");
            return View(request);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _shopService.DeleteShopAsync(id);
            TempData["SuccessMessage"] = "Shop deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting shop: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentUserId()
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        return int.TryParse(userIdString, out var userId) ? userId : 0;
    }

    private bool IsAuthenticated()
    {
        var isAuth = HttpContext.Session.GetString("IsAuthenticated");
        var userId = HttpContext.Session.GetString("UserId");
        return !string.IsNullOrEmpty(isAuth) && isAuth == "true" && !string.IsNullOrEmpty(userId);
    }
}

