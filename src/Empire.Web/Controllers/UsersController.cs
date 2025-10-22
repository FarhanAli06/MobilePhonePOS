using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.User;
using Empire.Domain.Enums;
using Empire.Web.Authorization;

namespace Empire.Web.Controllers;

[SessionAuthorize]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IShopService _shopService;

    public UsersController(IUserService userService, IShopService shopService)
    {
        _userService = userService;
        _shopService = shopService;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = GetCurrentUserId();
        var currentShopId = GetCurrentShopId();
        
        // Get users for current shop or all users if super admin
        var users = currentShopId > 0 
            ? await _userService.GetUsersByShopAsync(currentShopId)
            : await _userService.GetAllUsersAsync();

        return View(users);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateViewBags();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            await PopulateViewBags();
            return View(request);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            await _userService.CreateUserAsync(request, currentUserId);
            
            TempData["SuccessMessage"] = "User created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error creating user: {ex.Message}");
            await PopulateViewBags();
            return View(request);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var updateRequest = new UpdateUserRequest
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            IsActive = user.IsActive,
            ShopId = user.ShopRoles.FirstOrDefault()?.ShopId ?? 0,
            Role = user.ShopRoles.FirstOrDefault()?.Role ?? UserRole.Technician
        };

        await PopulateViewBags();
        ViewBag.UserId = id;
        ViewBag.Username = user.Username;
        
        return View(updateRequest);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            await PopulateViewBags();
            ViewBag.UserId = id;
            var user = await _userService.GetUserByIdAsync(id);
            ViewBag.Username = user?.Username ?? "";
            return View(request);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            await _userService.UpdateUserAsync(id, request, currentUserId);
            
            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error updating user: {ex.Message}");
            await PopulateViewBags();
            ViewBag.UserId = id;
            var user = await _userService.GetUserByIdAsync(id);
            ViewBag.Username = user?.Username ?? "";
            return View(request);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _userService.DeleteUserAsync(id);
            TempData["SuccessMessage"] = "User deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CheckUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return Json(true);
        }

        var exists = await _userService.UsernameExistsAsync(username);
        return Json(!exists);
    }

    [HttpGet]
    public async Task<IActionResult> CheckEmail(string email, int? id = null)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Json(true);
        }

        var exists = await _userService.EmailExistsAsync(email);
        
        // If editing existing user, allow same email
        if (exists && id.HasValue)
        {
            var user = await _userService.GetUserByIdAsync(id.Value);
            if (user != null && user.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                return Json(true);
            }
        }

        return Json(!exists);
    }

    private async Task PopulateViewBags()
    {
        var currentUserId = GetCurrentUserId();
        var shops = await _shopService.GetShopsByUserAsync(currentUserId);
        
        ViewBag.Shops = new SelectList(shops, "Id", "Name");
        ViewBag.Roles = new SelectList(Enum.GetValues<UserRole>().Select(r => new { 
            Value = (int)r, 
            Text = r.ToString() 
        }), "Value", "Text");
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
}

