using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Empire.Web.Authorization;
using Empire.Web.DTOs.User;
using Empire.Web.Services.User;
using Empire.Web.Services.Shop;
using Empire.Web.Services;
using Empire.Web.Services.Role;

namespace Empire.Web.Controllers;

[SessionAuthorizeWithShop]
public class UsersController : BaseController
{
    private readonly IUserApiService _userService;
    private readonly IShopApiService _shopService;
    private readonly IRoleApiService _roleService;
    private readonly IMapper _mapper;

    public UsersController(
        IUserApiService userService, 
        IShopApiService shopService,
        IRoleApiService roleService,
        IMapper mapper,
        ITimezoneService timezoneService, 
        ILogger<UsersController> logger)
        : base(logger, timezoneService)
    {
        _userService = userService;
        _shopService = shopService;
        _roleService = roleService;
        _mapper = mapper;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = GetCurrentUserId();
        var currentShopId = GetCurrentShopId();
        
        // Get users for current shop or all users if super admin
        var users = currentShopId > 0 
            ? await _userService.GetByShopAsync(currentShopId)
            : await _userService.GetAllAsync();

        return View(users);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateViewBags();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            await PopulateViewBags();
            return View(request);
        }

        try
        {
            await _userService.CreateAsync(request);
            
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
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Map user to update request using AutoMapper
        var updateRequest = _mapper.Map<UpdateUserRequestDto>(user);

        await PopulateViewBags();
        ViewBag.UserId = id;
        ViewBag.Username = user.Username;
        
        return View(updateRequest);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateUserRequestDto request)
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
            await _userService.UpdateAsync(id, request);
            
            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error updating user: {ex.Message}");
            await PopulateViewBags();
            ViewBag.UserId = id;
            var user = await _userService.GetByIdAsync(id);
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
            await _userService.DeleteAsync(id);
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
            var user = await _userService.GetByIdAsync(id.Value);
            if (user != null && user.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                return Json(true);
            }
        }

        return Json(!exists);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var users = currentShopId > 0
                ? await _userService.GetByShopAsync(currentShopId)
                : await _userService.GetAllAsync();

            var result = users.Select(u => new
            {
                id         = u.Id,
                username   = u.Username,
                email      = u.Email,
                firstName  = u.FirstName,
                lastName   = u.LastName,
                isActive   = u.IsActive,
                roleName   = currentShopId > 0
                    ? (u.ShopRoles.FirstOrDefault(r => r.ShopId == currentShopId)?.RoleName
                       ?? u.ShopRoles.FirstOrDefault(r => r.ShopId == currentShopId)?.Role
                       ?? string.Empty)
                    : (u.ShopRoles.FirstOrDefault()?.RoleName
                       ?? u.ShopRoles.FirstOrDefault()?.Role
                       ?? string.Empty)
            });

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users for PagePermissions");
            return StatusCode(500, new { message = "Failed to load users." });
        }
    }

    private async Task PopulateViewBags()
    {
        var currentUserId = GetCurrentUserId();
        var shops = await _shopService.GetShopsByUserAsync(currentUserId);
        var roles = await _roleService.GetActiveAsync();
        
        ViewBag.Shops = new SelectList(shops, "Id", "Name");
        ViewBag.Roles = new SelectList(roles, "Id", "Name");
    }
}

