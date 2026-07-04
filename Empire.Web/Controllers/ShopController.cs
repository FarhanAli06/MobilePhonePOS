using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Empire.Web.Authorization;
using Empire.Web.DTOs.Shop;
using Empire.Web.Services.Shop;
using Empire.Web.Services.User;
using Empire.Web.Services;

namespace Empire.Web.Controllers;

[SessionAuthorizeWithShop]
public class ShopController : BaseController
{
    private readonly IShopApiService _shopService;
    private readonly IUserApiService _userService;
    private readonly IMapper _mapper;

    public ShopController(
        IShopApiService shopService, 
        IUserApiService userService, 
        IMapper mapper,
        ITimezoneService timezoneService, 
        ILogger<ShopController> logger)
        : base(logger, timezoneService)
    {
        _shopService = shopService;
        _userService = userService;
        _mapper = mapper;
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
    public async Task<IActionResult> Create(CreateShopRequestDto request, IFormFile? logoFile)
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
            
            // Handle logo upload
            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
                Directory.CreateDirectory(uploadsFolder);
                
                var uniqueFileName = $"{Guid.NewGuid()}_{logoFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(fileStream);
                }
                
                request.LogoPath = $"/uploads/logos/{uniqueFileName}";
            }
            
            // Create shop first
            var shop = await _shopService.CreateShopAsync(request, userId);
            
            // Then assign the creator as Manager of the shop
            await _userService.AssignUserToShopAsync(userId, shop.Id, "Manager");
            
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

            // Map shop to update request using AutoMapper
            var updateRequest = _mapper.Map<UpdateShopRequestDto>(shop);

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
    public async Task<IActionResult> Edit(int id, UpdateShopRequestDto request, IFormFile? logoFile)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            var userId = GetCurrentUserId();
            
            // Handle logo upload
            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
                Directory.CreateDirectory(uploadsFolder);
                
                var uniqueFileName = $"{Guid.NewGuid()}_{logoFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(fileStream);
                }
                
                request.LogoPath = $"/uploads/logos/{uniqueFileName}";
            }
            
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

    private bool IsAuthenticated()
    {
        var isAuth = HttpContext.Session.GetString("IsAuthenticated");
        var userId = HttpContext.Session.GetString("UserId");
        return !string.IsNullOrEmpty(isAuth) && isAuth == "true" && !string.IsNullOrEmpty(userId);
    }
}

