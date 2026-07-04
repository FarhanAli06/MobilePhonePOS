using Empire.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empire.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : BaseApiController
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var roles = await _roleService.GetAllAsync();
            return SuccessResponse(roles, "Roles retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return ErrorResponse("An error occurred while retrieving roles", 500);
        }
    }

    /// <summary>
    /// Get all active roles
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var roles = await _roleService.GetActiveRolesAsync();
            return SuccessResponse(roles, "Active roles retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active roles");
            return ErrorResponse("An error occurred while retrieving active roles", 500);
        }
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var role = await _roleService.GetByIdAsync(id);
            if (role == null)
                return NotFoundResponse($"Role with ID {id} not found");
            return SuccessResponse(role, "Role retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {Id}", id);
            return ErrorResponse("An error occurred while retrieving the role", 500);
        }
    }
}
