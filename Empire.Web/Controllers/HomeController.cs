using Microsoft.AspNetCore.Mvc;
using Empire.Web.Models;
using Empire.Web.Services.Auth;
using Empire.Web.Services.Home;
using Empire.Web.Services.Dashboard;
using Empire.Web.DTOs.Auth;
using Empire.Web.Services;
using Empire.Web.Services.Session;
using Empire.Web.Services.PagePermission;
using Empire.Web.Constants;

namespace Empire.Web.Controllers;

public class HomeController : BaseController
{
    private readonly IAuthApiService _authService;
    private readonly IDashboardService _dashboardService;
    private readonly ITokenStorageService _tokenStorageService;
    private readonly ISessionHelper _sessionHelper;
    private readonly IPagePermissionApiService _pagePermissionService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IAuthApiService authService,
        IDashboardService dashboardService,
        ITokenStorageService tokenStorageService,
        ISessionHelper sessionHelper,
        IPagePermissionApiService pagePermissionService,
        ILogger<HomeController> logger,
        ITimezoneService timezoneService) : base(logger, timezoneService)
    {
        _authService = authService;
        _dashboardService = dashboardService;
        _tokenStorageService = tokenStorageService;
        _sessionHelper = sessionHelper;
        _pagePermissionService = pagePermissionService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (_sessionHelper.IsAuthenticated())
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (_sessionHelper.IsAuthenticated())
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var loginRequest = new LoginRequestDto
            {
                Username = model.Username,
                Password = model.Password
            };

            var result = await _authService.LoginAsync(loginRequest);

            if (result != null && result.User != null)
            {
                // Store JWT token in HttpOnly cookie (used by AuthenticationDelegatingHandler)
                _tokenStorageService.SetToken(result.AccessToken);

                // ── Populate session so SessionAuthorize and _Layout work ──────────
                var session = HttpContext.Session;
                session.SetString(SessionKeys.IsAuthenticated, "true");
                session.SetString(SessionKeys.UserId,          result.User.Id.ToString());
                session.SetString(SessionKeys.Username,        result.User.Username);
                // Layout uses "UserName" key (with capital N)
                session.SetString("UserName",                  result.User.Username);
                session.SetString(SessionKeys.UserEmail,       result.User.Email);
                session.SetString(SessionKeys.AuthToken,       result.AccessToken);

                // Populate shop / role from the first shop-role entry
                var defaultShop = result.ShopRoles.FirstOrDefault();
                if (defaultShop != null)
                {
                    session.SetString(SessionKeys.CurrentShopId,   defaultShop.ShopId.ToString());
                    session.SetString(SessionKeys.CurrentShopName, defaultShop.ShopName);
                    session.SetString(SessionKeys.UserRole,        defaultShop.RoleName);
                }
                else if (result.User.ShopRoles?.Any() == true)
                {
                    var firstRole = result.User.ShopRoles.First();
                    session.SetString(SessionKeys.CurrentShopId,   firstRole.ShopId.ToString());
                    session.SetString(SessionKeys.CurrentShopName, firstRole.ShopName);
                    session.SetString(SessionKeys.UserRole,        firstRole.RoleName);
                }
                // ── Load and store granted page keys in session ───────────────────
                try
                {
                    // Pass the token explicitly: at this point in the login request the token
                    // has just been issued and is not yet readable from session/cookie by the
                    // DelegatingHandler (session not committed, cookie not yet in Request.Cookies).
                    // shopId is now stored in session; also pass it explicitly for the token-based overload
                    int.TryParse(session.GetString(SessionKeys.CurrentShopId), out var loginShopId);
                    var pageKeys = await _pagePermissionService.GetGrantedPageKeysAsync(
                        result.User.Id, loginShopId, result.AccessToken);
                    session.SetString(SessionKeys.GrantedPageKeys, string.Join(",", pageKeys));
                    _logger.LogInformation(
                        "Loaded {Count} page keys for user {UserId}",
                        pageKeys.Count, result.User.Id);
                }
                catch (Exception pex)
                {
                    _logger.LogWarning(pex, "Could not load page permissions for user {UserId}", result.User.Id);
                    // Non-fatal: sidebar will be empty but login still succeeds
                    session.SetString(SessionKeys.GrantedPageKeys, string.Empty);
                }
                // ─────────────────────────────────────────────────────────────────

                _logger.LogInformation("User {Username} logged in successfully", result.User.Username);

                // Check if user has shop access
                var hasShops = result.ShopRoles.Any()
                    || (result.User.ShopRoles?.Any() == true);

                if (hasShops)
                {
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    TempData["InfoMessage"] = "Welcome! Please create your first shop to get started.";
                    return RedirectToAction("Create", "Shop");
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
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        // Clear JWT token cookie
        _tokenStorageService.RemoveToken();

        // Clear session
        _sessionHelper.ClearSession();

        _logger.LogInformation("User logged out successfully");

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Dashboard()
    {
        try
        {
            // Check authentication via session
            if (!_sessionHelper.IsAuthenticated())
            {
                return RedirectToAction("Login");
            }

            var userId = _sessionHelper.GetCurrentUserId();
            if (userId == 0)
            {
                _sessionHelper.ClearSession();
                return RedirectToAction("Login");
            }

            // Validate shop access and get current shop
            var currentShopId = _sessionHelper.GetCurrentShopId();
            var shopValidation = await _dashboardService.ValidateAndSetCurrentShopAsync(userId, currentShopId);

            if (!shopValidation.IsValid)
            {
                if (shopValidation.NeedsShopCreation)
                {
                    TempData["InfoMessage"] = shopValidation.ErrorMessage;
                    return RedirectToAction("Create", "Shop");
                }

                _logger.LogError("Shop validation failed for user {UserId}: {Error}", userId, shopValidation.ErrorMessage);
                return RedirectToAction("Login");
            }

            // Update session if shop changed
            if (shopValidation.ShopId != currentShopId)
            {
                _sessionHelper.UpdateCurrentShop(shopValidation.ShopId, shopValidation.ShopName);
            }

            // Load dashboard data from service
            var model = await _dashboardService.GetDashboardDataAsync(shopValidation.ShopId);

            // Set display properties from session
            model.CurrentShopName = shopValidation.ShopName;
            model.UserName        = _sessionHelper.GetCurrentUserName();
            model.UserRole        = _sessionHelper.GetCurrentUserRole();

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            ViewBag.Error = "Error loading dashboard data";
            return View(new DashboardViewModel());
        }
    }
}
