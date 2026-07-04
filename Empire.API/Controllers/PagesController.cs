using Microsoft.AspNetCore.Mvc;
using Empire.Application.DTOs.Page;
using Empire.Application.Interfaces;

namespace Empire.API.Controllers;

/// <summary>
/// Manages the master list of application pages and user page-permission assignments.
/// </summary>
[Route("api/[controller]")]
public class PagesController : BaseApiController
{
    private readonly IPagePermissionService _pagePermissionService;

    public PagesController(IPagePermissionService pagePermissionService)
    {
        _pagePermissionService = pagePermissionService;
    }

    // ── Page catalogue ─────────────────────────────────────────────────────

    /// <summary>GET api/pages — flat list of all active pages.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pages = await _pagePermissionService.GetAllPagesAsync();
        return SuccessResponse(pages);
    }

    /// <summary>GET api/pages/tree — pages structured as parent/child tree.</summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var tree = await _pagePermissionService.GetPageTreeAsync();
        return SuccessResponse(tree);
    }

    // ── User permission queries ────────────────────────────────────────────

    /// <summary>
    /// GET api/pages/user/{userId}?shopId={shopId} — all pages with an IsActive flag
    /// indicating whether the user has been granted access (used by the admin assignment UI).
    /// shopId is read from JWT claims first; the query-string value is used as a fallback.
    /// </summary>
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetPagesForUser(int userId, [FromQuery] int shopId = 0)
    {
        var resolvedShopId = ResolveShopId(shopId);
        var pages = await _pagePermissionService.GetPagesWithUserPermissionsAsync(userId, resolvedShopId);
        return SuccessResponse(pages);
    }

    /// <summary>
    /// GET api/pages/user/{userId}/summary?shopId={shopId} — full permission summary.
    /// </summary>
    [HttpGet("user/{userId:int}/summary")]
    public async Task<IActionResult> GetUserPermissionSummary(int userId, [FromQuery] int shopId = 0)
    {
        var resolvedShopId = ResolveShopId(shopId);
        var summary = await _pagePermissionService.GetUserPermissionSummaryAsync(userId, resolvedShopId);
        if (summary == null)
            return NotFoundResponse($"User {userId} not found.");

        return SuccessResponse(summary);
    }

    /// <summary>
    /// GET api/pages/user/{userId}/keys?shopId={shopId} — flat list of page keys the user can access.
    /// Called immediately after login to populate the session.
    /// </summary>
    [HttpGet("user/{userId:int}/keys")]
    public async Task<IActionResult> GetGrantedPageKeys(int userId, [FromQuery] int shopId = 0)
    {
        var resolvedShopId = ResolveShopId(shopId);
        var keys = await _pagePermissionService.GetGrantedPageKeysAsync(userId, resolvedShopId);
        return SuccessResponse(keys);
    }

    // ── Permission assignment ──────────────────────────────────────────────

    /// <summary>
    /// POST api/pages/assign — replace a user's page assignments.
    /// Sends the complete desired list; any page not in the list is revoked.
    /// </summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignPages([FromBody] AssignUserPagesRequest request)
    {
        if (request == null || request.UserId <= 0)
            return ValidationErrorResponse("UserId is required.");

        // Determine who is making the change (from JWT claims or fallback to 0)
        var grantedByStr = User.FindFirst("UserId")?.Value ?? "0";
        int.TryParse(grantedByStr, out var grantedBy);

        // ShopId: prefer JWT claim, fall back to the value in the request body
        var shopId = ResolveShopId(request.ShopId);
        if (shopId <= 0)
            return ValidationErrorResponse("A valid shop context is required to assign page permissions.");

        var success = await _pagePermissionService.AssignPagesToUserAsync(
            request.UserId, shopId, request.PageIds, grantedBy);

        if (!success)
            return NotFoundResponse($"User {request.UserId} not found.");

        return SuccessResponse("Page permissions updated successfully.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the shopId from JWT claims if available and non-zero;
    /// otherwise falls back to the provided fallback value.
    /// </summary>
    private int ResolveShopId(int fallback)
    {
        var fromClaim = GetCurrentShopId();
        return fromClaim > 0 ? fromClaim : fallback;
    }
}
