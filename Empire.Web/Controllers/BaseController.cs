using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Empire.Web.Constants;
using Empire.Web.Services;
using Empire.Web.Services.Lookup;

namespace Empire.Web.Controllers;

/// <summary>
/// Base controller providing common functionality for all controllers.
/// User context is read from HttpContext.Session (session-based auth).
/// The JWT token is only used as a bearer token when calling the API —
/// there is no ASP.NET Core JWT middleware configured in the Web app.
/// </summary>
public abstract class BaseController : Controller
{
    protected readonly ILogger _logger;
    protected readonly ITimezoneService _timezoneService;
    protected readonly ILookupApiService? _lookupApiService;

    protected BaseController(ILogger logger, ITimezoneService timezoneService)
    {
        _logger = logger;
        _timezoneService = timezoneService;
    }

    protected BaseController(ILogger logger, ITimezoneService timezoneService, ILookupApiService lookupApiService)
    {
        _logger = logger;
        _timezoneService = timezoneService;
        _lookupApiService = lookupApiService;
    }

    // ─── Session helper ───────────────────────────────────────────────────────

    /// <summary>Safe accessor for HttpContext.Session (never throws).</summary>
    private ISession? TryGetSession()
    {
        try { return HttpContext?.Session; }
        catch { return null; }
    }

    private string SessionGet(string key)
        => TryGetSession()?.GetString(key) ?? string.Empty;

    // ─── Timezone Methods ─────────────────────────────────────────────────────

    protected DateTime GetUserNow()               => _timezoneService.GetUserNow();
    protected DateTime ToUserTime(DateTime utc)   => _timezoneService.ConvertToUserTime(utc);
    protected DateTime ToUtc(DateTime local)      => _timezoneService.ConvertToUtc(local);

    // ─── User / Shop context (from Session) ──────────────────────────────────

    /// <summary>Returns the current user's ID stored in session after login.</summary>
    protected int GetCurrentUserId()
    {
        var val = SessionGet(SessionKeys.UserId);
        return int.TryParse(val, out var id) ? id : 0;
    }

    /// <summary>Returns the current shop ID stored in session after login.</summary>
    protected int GetCurrentShopId()
    {
        var val = SessionGet(SessionKeys.CurrentShopId);
        return int.TryParse(val, out var id) ? id : 0;
    }

    protected string GetCurrentUserRole()     => SessionGet(SessionKeys.UserRole);
    protected string GetCurrentUsername()     => SessionGet(SessionKeys.Username);
    protected string GetCurrentUserEmail()    => SessionGet(SessionKeys.UserEmail);
    protected string GetCurrentShopName()     => SessionGet(SessionKeys.CurrentShopName);

    protected string GetCurrentUserFirstName()
    {
        var full = SessionGet("UserFullName");
        if (!string.IsNullOrEmpty(full)) return full.Split(' ')[0];
        return SessionGet(SessionKeys.Username);
    }

    protected string GetCurrentUserLastName()
    {
        var full = SessionGet("UserFullName");
        if (!string.IsNullOrEmpty(full))
        {
            var parts = full.Split(' ');
            return parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;
        }
        return string.Empty;
    }

    protected string GetCurrentUserFullName()
    {
        var full = SessionGet("UserFullName");
        if (!string.IsNullOrEmpty(full)) return full;
        var first = GetCurrentUserFirstName();
        var last  = GetCurrentUserLastName();
        return string.IsNullOrEmpty(last) ? first : $"{first} {last}";
    }

    protected int GetCurrentRoleId()
    {
        var val = SessionGet("CurrentRoleId");
        return int.TryParse(val, out var id) ? id : 0;
    }

    // ─── Authentication Checks ────────────────────────────────────────────────

    /// <summary>
    /// Returns true when the session contains a valid IsAuthenticated=true flag and a UserId.
    /// This is the session-based equivalent of User.Identity.IsAuthenticated.
    /// </summary>
    protected bool IsAuthenticated()
    {
        var flag   = SessionGet(SessionKeys.IsAuthenticated);
        var userId = SessionGet(SessionKeys.UserId);
        return flag == "true" && !string.IsNullOrEmpty(userId);
    }

    protected bool IsManager()
    {
        var role = GetCurrentUserRole();
        return role.Equals("Manager",    StringComparison.OrdinalIgnoreCase) ||
               role.Equals("Admin",      StringComparison.OrdinalIgnoreCase) ||
               role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    protected bool IsAdmin()
    {
        var role = GetCurrentUserRole();
        return role.Equals("Admin",      StringComparison.OrdinalIgnoreCase) ||
               role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Permission check — always true for SuperAdmin, otherwise false (extend as needed).</summary>
    protected bool HasPermission(string permission)
    {
        return IsAdmin();
    }

    /// <summary>Shop access check — always true for SuperAdmin, otherwise checks session.</summary>
    protected bool HasShopAccess(int shopId)
    {
        if (IsAdmin()) return true;
        return GetCurrentShopId() == shopId;
    }

    // ─── Common Redirects ─────────────────────────────────────────────────────

    protected IActionResult RedirectToLogin()
        => RedirectToAction(RouteConstants.Actions.Login, RouteConstants.Controllers.Home);

    protected IActionResult RedirectToUnauthorized()
        => RedirectToAction(RouteConstants.Views.Unauthorized, RouteConstants.Controllers.Home);

    protected IActionResult RedirectToDashboard()
        => RedirectToAction(RouteConstants.Actions.Index, RouteConstants.Controllers.Dashboard);

    // ─── Lookup Helper Methods ────────────────────────────────────────────────

    protected async Task<List<Empire.Web.DTOs.Lookup.LookupValueDto>?> GetLookupValuesAsync(string category)
    {
        if (_lookupApiService == null)
        {
            _logger.LogWarning("LookupApiService is not available in this controller");
            return new List<Empire.Web.DTOs.Lookup.LookupValueDto>();
        }

        try
        {
            var lookups = await _lookupApiService.GetByCategoryAsync(category);
            return lookups?.Where(l => l.IsActive).OrderBy(l => l.DisplayOrder).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lookup values for category: {Category}", category);
            return new List<Empire.Web.DTOs.Lookup.LookupValueDto>();
        }
    }

    protected async Task<List<SelectListItem>> GetLookupSelectListAsync(string category, string? selectedValue = null)
    {
        var lookups = await GetLookupValuesAsync(category);
        if (lookups == null || !lookups.Any())
            return new List<SelectListItem>();

        return lookups.Select(l => new SelectListItem
        {
            Value    = l.Value,
            Text     = l.Value,
            Selected = l.Value == selectedValue
        }).ToList();
    }

    protected async Task LoadCommonLookupsAsync()
    {
        if (_lookupApiService == null) return;

        try
        {
            ViewBag.PaymentStatuses = await GetLookupSelectListAsync(LookupCategories.PaymentStatus);
            ViewBag.PaymentMethods  = await GetLookupSelectListAsync(LookupCategories.PaymentMethod);
            ViewBag.RepairStatuses  = await GetLookupSelectListAsync(LookupCategories.RepairStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading common lookups");
        }
    }

    // ─── ViewBag Helpers ──────────────────────────────────────────────────────

    protected void SetUserInfoInViewBag()
    {
        ViewBag.Username        = GetCurrentUsername();
        ViewBag.UserEmail       = GetCurrentUserEmail();
        ViewBag.UserFullName    = GetCurrentUserFullName();
        ViewBag.CurrentShopName = GetCurrentShopName();
        ViewBag.CurrentRole     = GetCurrentUserRole();
        ViewBag.IsAuthenticated = IsAuthenticated();
        ViewBag.IsAdmin         = IsAdmin();
        ViewBag.IsManager       = IsManager();
    }

    // ─── Error / Message Helpers ──────────────────────────────────────────────

    protected void HandleApiError(string operation, Exception ex)
    {
        _logger.LogError(ex, "Error during {Operation}: {Message}", operation, ex.Message);
        TempData["ErrorMessage"] = $"An error occurred during {operation}. Please try again.";
    }

    protected void SetSuccessMessage(string message) => TempData["SuccessMessage"] = message;
    protected void SetErrorMessage(string message)   => TempData["ErrorMessage"]   = message;
    protected void SetWarningMessage(string message) => TempData["WarningMessage"] = message;
    protected void SetInfoMessage(string message)    => TempData["InfoMessage"]    = message;
}
