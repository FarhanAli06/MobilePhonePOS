using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Empire.API.Controllers;

/// <summary>
/// Base controller for all API controllers with common functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Get the current user ID from JWT claims
    /// </summary>
    protected int GetCurrentUserId()
    {
        // Try 'sub' claim first (JWT standard), then fallback to NameIdentifier
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get the current shop ID from JWT claims
    /// </summary>
    protected int GetCurrentShopId()
    {
        var shopIdClaim = User.FindFirst("CurrentShopId")?.Value;
        return int.TryParse(shopIdClaim, out var shopId) ? shopId : 0;
    }

    /// <summary>
    /// Get the current user's role from JWT claims
    /// </summary>
    protected string GetCurrentUserRole()
    {
        return User.FindFirst("CurrentRole")?.Value ?? string.Empty;
    }

    /// <summary>
    /// Get the current username from JWT claims
    /// </summary>
    protected string GetCurrentUsername()
    {
        return User.FindFirst("unique_name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Get the current user's email from JWT claims
    /// </summary>
    protected string GetCurrentUserEmail()
    {
        return User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Get the current user's first name from JWT claims
    /// </summary>
    protected string GetCurrentUserFirstName()
    {
        return User.FindFirst("FirstName")?.Value ?? string.Empty;
    }

    /// <summary>
    /// Get the current user's last name from JWT claims
    /// </summary>
    protected string GetCurrentUserLastName()
    {
        return User.FindFirst("LastName")?.Value ?? string.Empty;
    }

    /// <summary>
    /// Get the current user's full name from JWT claims
    /// </summary>
    protected string GetCurrentUserFullName()
    {
        return User.FindFirst("FullName")?.Value ?? $"{GetCurrentUserFirstName()} {GetCurrentUserLastName()}";
    }

    /// <summary>
    /// Get the current shop name from JWT claims
    /// </summary>
    protected string GetCurrentShopName()
    {
        return User.FindFirst("CurrentShopName")?.Value ?? string.Empty;
    }

    /// <summary>
    /// Get the current role ID from JWT claims
    /// </summary>
    protected int GetCurrentRoleId()
    {
        var roleIdClaim = User.FindFirst("CurrentRoleId")?.Value;
        return int.TryParse(roleIdClaim, out var roleId) ? roleId : 0;
    }

    /// <summary>
    /// Check if user has access to a specific shop
    /// </summary>
    protected bool HasShopAccess(int shopId)
    {
        return User.HasClaim(c => c.Type == $"ShopId_{shopId}" && c.Value == "true");
    }

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    protected bool IsAuthenticated()
    {
        return User.Identity?.IsAuthenticated ?? false;
    }

    /// <summary>
    /// Check if user has a specific role in current shop
    /// </summary>
    protected bool HasRole(string roleName)
    {
        return GetCurrentUserRole().Equals(roleName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if user is admin or manager
    /// </summary>
    protected bool IsAdminOrManager()
    {
        var role = GetCurrentUserRole();
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
               role.Equals("Manager", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Create a success response with data
    /// </summary>
    protected IActionResult SuccessResponse<T>(T data, string? message = null)
    {
        return Ok(new
        {
            success = true,
            message = message ?? "Operation completed successfully",
            data,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Create a success response without data
    /// </summary>
    protected IActionResult SuccessResponse(string message = "Operation completed successfully")
    {
        return Ok(new
        {
            success = true,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    protected IActionResult ErrorResponse(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new
        {
            success = false,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Create a not found response
    /// </summary>
    protected IActionResult NotFoundResponse(string message = "Resource not found")
    {
        return NotFound(new
        {
            success = false,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Create an unauthorized response
    /// </summary>
    protected IActionResult UnauthorizedResponse(string message = "Unauthorized access")
    {
        return Unauthorized(new
        {
            success = false,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Create a validation error response
    /// </summary>
    protected IActionResult ValidationErrorResponse(string message = "Validation failed")
    {
        var errors = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

        return BadRequest(new
        {
            success = false,
            message,
            errors,
            timestamp = DateTime.UtcNow
        });
    }
}
