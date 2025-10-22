using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Auth;
using Empire.Web.Models;
using System.Security.Claims;

namespace Empire.Web.Controllers;

public class HomeController : Controller
{
    private readonly IAuthService _authService;
    private readonly IRepairService _repairService;
    private readonly IInventoryService _inventoryService;
    private readonly IShopService _shopService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IAuthService authService,
        IRepairService repairService,
        IInventoryService inventoryService,
        IShopService shopService,
        ILogger<HomeController> logger)
    {
        _authService = authService;
        _repairService = repairService;
        _inventoryService = inventoryService;
        _shopService = shopService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (IsAuthenticated())
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (IsAuthenticated())
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var loginRequest = new LoginRequest
            {
                Username = model.Username,
                Password = model.Password
            };

            var result = await _authService.LoginAsync(loginRequest);
            
            if (result != null && result.User != null)
            {
                // Store user information in session
                HttpContext.Session.SetString("UserId", result.User.Id.ToString());
                HttpContext.Session.SetString("Username", result.User.Username);
                HttpContext.Session.SetString("UserName", $"{result.User.FirstName} {result.User.LastName}");
                HttpContext.Session.SetString("AccessToken", result.AccessToken);
                HttpContext.Session.SetString("IsAuthenticated", "true");
                
                // Store shop roles if available
                if (result.User.ShopRoles?.Any() == true)
                {
                    var firstShop = result.User.ShopRoles.First();
                    HttpContext.Session.SetInt32("CurrentShopId", firstShop.ShopId);
                    HttpContext.Session.SetString("CurrentShopName", firstShop.ShopName);
                    HttpContext.Session.SetString("UserRole", firstShop.Role.ToString());
                    
                    // Redirect to dashboard if user has shops
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    // Try to get user's shops from the database
                    var userShops = await _shopService.GetShopsByUserAsync(result.User.Id);
                    if (userShops.Any())
                    {
                        var firstShop = userShops.First();
                        HttpContext.Session.SetInt32("CurrentShopId", firstShop.Id);
                        HttpContext.Session.SetString("CurrentShopName", firstShop.Name);
                        HttpContext.Session.SetString("UserRole", "Admin"); // Default role
                        
                        return RedirectToAction("Dashboard");
                    }
                    else
                    {
                        // Super admin or user without shops - redirect to shop creation
                        HttpContext.Session.SetString("UserRole", "SuperAdmin");
                        TempData["InfoMessage"] = "Welcome! Please create your first shop to get started.";
                        return RedirectToAction("Create", "Shop");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for user {Username}", model.Username);
            ModelState.AddModelError("", "An error occurred during login. Please try again.");
            return View(model);
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Dashboard()
    {
        try
        {
            // Check authentication using session
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            // Check if user has any shops
            var userShops = await _shopService.GetShopsByUserAsync(userId);
            if (!userShops.Any())
            {
                TempData["InfoMessage"] = "Welcome! Please create your first shop to get started.";
                return RedirectToAction("Create", "Shop");
            }

            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                // Set the first shop as current if none is set
                var firstShop = userShops.First();
                HttpContext.Session.SetString("CurrentShopId", firstShop.Id.ToString());
                HttpContext.Session.SetString("CurrentShopName", firstShop.Name);
                currentShopId = firstShop.Id;
            }

            var model = new DashboardViewModel
            {
                CurrentShopName = HttpContext.Session.GetString("CurrentShopName") ?? "Unknown Shop",
                UserName = HttpContext.Session.GetString("UserName") ?? "User",
                UserRole = HttpContext.Session.GetString("UserRole") ?? "User"
            };

            // Load dashboard statistics
            var repairsFilter = new Empire.Application.DTOs.Repair.RepairFilterRequest
            {
                ShopId = currentShopId
            };
            var repairs = await _repairService.GetRepairsAsync(repairsFilter);
            
            var inventoryFilter = new Empire.Application.DTOs.Inventory.InventoryFilterRequest
            {
                ShopId = currentShopId
            };
            var inventory = await _inventoryService.GetInventoryAsync(inventoryFilter);
            var lowStockItems = await _inventoryService.GetLowStockItemsAsync(currentShopId);

            model.TotalRepairs = repairs.Count();
            model.InProgressRepairs = repairs.Count(r => r.Status == "InProgress");
            model.CompletedRepairs = repairs.Count(r => r.Status == "Completed" || r.Status == "Done");
            model.TotalInventoryValue = inventory.Sum(i => i.Stock * i.RetailPrice);
            model.LowStockItemsCount = lowStockItems.Count();
            
            model.RecentRepairs = repairs.OrderByDescending(r => r.CreatedDate).Take(5).ToList();
            model.LowStockItems = lowStockItems.Take(5).ToList();

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            ViewBag.Error = "Error loading dashboard data";
            return View(new DashboardViewModel());
        }
    }

    private int GetCurrentUserId()
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        return int.TryParse(userIdString, out var userId) ? userId : 0;
    }

    private int GetCurrentShopId()
    {
        var shopIdString = HttpContext.Session.GetString("CurrentShopId");
        return int.TryParse(shopIdString, out var shopId) ? shopId : 0;
    }

    private bool IsAuthenticated()
    {
        var isAuth = HttpContext.Session.GetString("IsAuthenticated");
        var userId = HttpContext.Session.GetString("UserId");
        return !string.IsNullOrEmpty(isAuth) && isAuth == "true" && !string.IsNullOrEmpty(userId);
    }
}

