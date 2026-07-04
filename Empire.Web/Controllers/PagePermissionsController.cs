using Microsoft.AspNetCore.Mvc;
using Empire.Web.Authorization;
using Empire.Web.Services.PagePermission;
using Empire.Web.Services;
using Empire.Web.Services.Session;
using Empire.Web.Constants;

namespace Empire.Web.Controllers;

/// <summary>
/// Admin UI for assigning application pages to individual users.
/// Only accessible to SuperAdmin and Admin roles.
/// </summary>
[SessionAuthorize]
public class PagePermissionsController : BaseController
{
    private readonly IPagePermissionApiService _pagePermissionService;
    private readonly ISessionHelper _sessionHelper;

    public PagePermissionsController(
        IPagePermissionApiService pagePermissionService,
        ISessionHelper sessionHelper,
        ILogger<PagePermissionsController> logger,
        ITimezoneService timezoneService) : base(logger, timezoneService)
    {
        _pagePermissionService = pagePermissionService;
        _sessionHelper = sessionHelper;
    }

    // ── Index: list all users ──────────────────────────────────────────────

    public IActionResult Index()
    {
        // The view will load users via the existing UsersController/UserApiService
        return View();
    }

    // ── Assign: show page assignment form for a specific user ──────────────

    [HttpGet]
    public async Task<IActionResult> Assign(int userId)
    {
        if (userId <= 0)
        {
            TempData["ErrorMessage"] = "Invalid user ID.";
            return RedirectToAction("Index");
        }

        var shopId = GetCurrentShopId();
        var pages   = await _pagePermissionService.GetPagesForUserAsync(userId, shopId);
        var summary = await _pagePermissionService.GetUserPermissionSummaryAsync(userId, shopId);

        ViewBag.UserId   = userId;
        ViewBag.ShopId   = shopId;
        ViewBag.UserName = summary?.FullName ?? summary?.Username ?? $"User #{userId}";
        ViewBag.Pages    = pages;

        return View();
    }

    // ── Save: process the page assignment form ─────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int userId, [FromForm] List<int> pageIds)
    {
        if (userId <= 0)
        {
            TempData["ErrorMessage"] = "Invalid user ID.";
            return RedirectToAction("Index");
        }

        var shopId  = GetCurrentShopId();
        var success = await _pagePermissionService.AssignPagesToUserAsync(userId, shopId, pageIds ?? new List<int>());

        if (success)
        {
            // If the admin just updated their own permissions (or the target is the
            // currently logged-in user), refresh the session immediately so the
            // sidebar reflects the new assignment without requiring a re-login.
            var currentUserId = _sessionHelper.GetCurrentUserId();
            if (userId == currentUserId)
            {
                try
                {
                    var updatedKeys = await _pagePermissionService.GetGrantedPageKeysAsync(userId, shopId);
                    HttpContext.Session.SetString(
                        SessionKeys.GrantedPageKeys,
                        string.Join(",", updatedKeys));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not refresh GrantedPageKeys in session for user {UserId}", userId);
                }
            }

            TempData["SuccessMessage"] = userId == currentUserId
                ? "Page permissions updated successfully. Your sidebar has been refreshed."
                : "Page permissions updated successfully. The user must re-login to see the updated navigation.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update page permissions. Please try again.";
        }

        return RedirectToAction("Assign", new { userId });
    }

    // ── API: return current user's granted page keys (used by sidebar JS) ──

    [HttpGet]
    public async Task<IActionResult> MyPages()
    {
        var userId = _sessionHelper.GetCurrentUserId();
        if (userId == 0) return Json(new List<string>());

        var shopId = GetCurrentShopId();
        var keys   = await _pagePermissionService.GetGrantedPageKeysAsync(userId, shopId);
        return Json(keys);
    }
}
