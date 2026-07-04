using Empire.Application.DTOs.User;
using Empire.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empire.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            return SuccessResponse(users, "Users retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return ErrorResponse("An error occurred while retrieving users", 500);
        }
    }

    /// <summary>
    /// Get users by shop
    /// </summary>
    [HttpGet("shop/{shopId}")]
    public async Task<IActionResult> GetByShop(int shopId)
    {
        try
        {
            var users = await _userService.GetUsersByShopAsync(shopId);
            return SuccessResponse(users, "Users retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for shop {ShopId}", shopId);
            return ErrorResponse("An error occurred while retrieving users", 500);
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFoundResponse($"User with ID {id} not found");
            return SuccessResponse(user, "User retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {Id}", id);
            return ErrorResponse("An error occurred while retrieving the user", 500);
        }
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    [HttpGet("username/{username}")]
    public async Task<IActionResult> GetByUsername(string username)
    {
        try
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
                return NotFoundResponse($"User '{username}' not found");
            return SuccessResponse(user, "User retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by username {Username}", username);
            return ErrorResponse("An error occurred while retrieving the user", 500);
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationErrorResponse("Invalid user data");

        try
        {
            var createdByUserId = GetCurrentUserId();
            var user = await _userService.CreateUserAsync(request, createdByUserId);
            return SuccessResponse(user, "User created successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating user");
            return ErrorResponse(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return ErrorResponse("An error occurred while creating the user", 500);
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationErrorResponse("Invalid user data");

        try
        {
            var modifiedByUserId = GetCurrentUserId();
            var user = await _userService.UpdateUserAsync(id, request, modifiedByUserId);
            return SuccessResponse(user, "User updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {Id} not found for update", id);
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Id}", id);
            return ErrorResponse("An error occurred while updating the user", 500);
        }
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _userService.DeleteUserAsync(id);
            return SuccessResponse("User deleted successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {Id} not found for deletion", id);
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {Id}", id);
            return ErrorResponse("An error occurred while deleting the user", 500);
        }
    }

    /// <summary>
    /// Check if username exists
    /// </summary>
    [HttpGet("check-username")]
    public async Task<IActionResult> CheckUsername([FromQuery] string username)
    {
        try
        {
            var exists = await _userService.UsernameExistsAsync(username);
            return SuccessResponse(new { exists }, "Username check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username {Username}", username);
            return ErrorResponse("An error occurred while checking the username", 500);
        }
    }

    /// <summary>
    /// Check if email exists
    /// </summary>
    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail([FromQuery] string email)
    {
        try
        {
            var exists = await _userService.EmailExistsAsync(email);
            return SuccessResponse(new { exists }, "Email check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email {Email}", email);
            return ErrorResponse("An error occurred while checking the email", 500);
        }
    }
}
