using Microsoft.AspNetCore.Mvc;
using Empire.Web.Authorization;
using Empire.Web.DTOs.Styling;
using Empire.Web.Services.Styling;

namespace Empire.Web.Controllers;

/// <summary>
/// Web controller for the Styling admin page.
/// Proxies CRUD and assignment operations to the API's /api/styling endpoint.
/// </summary>
[SessionAuthorizeWithShop]
public class StylingController : Controller
{
    private readonly IStylingApiService _stylingApi;
    private readonly ILogger<StylingController> _logger;

    public StylingController(IStylingApiService stylingApi, ILogger<StylingController> logger)
    {
        _stylingApi = stylingApi;
        _logger = logger;
    }

    // ── Page ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.PageTitle = "Styling Management";
        return View();
    }

    // ── Styling CRUD ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _stylingApi.GetAllAsync();
            if (data == null)
                return Json(new { success = false, message = "Error retrieving stylings" });
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stylings");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSelections()
    {
        try
        {
            var data = await _stylingApi.GetSelectionsAsync();
            return Json(new { success = true, data = data ?? new List<StylingSelectionDto>() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting styling selections");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStylingRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data provided" });

            var isDuplicate = await _stylingApi.CheckDuplicateNameAsync(request.Name);
            if (isDuplicate)
                return Json(new { success = false, message = $"A styling named '{request.Name}' already exists" });

            var result = await _stylingApi.CreateAsync(request);
            if (result == null)
                return Json(new { success = false, message = "Error creating styling" });

            return Json(new { success = true, message = "Styling created successfully", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating styling");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStylingRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data provided" });

            var isDuplicate = await _stylingApi.CheckDuplicateNameAsync(request.Name, id);
            if (isDuplicate)
                return Json(new { success = false, message = $"A styling named '{request.Name}' already exists" });

            var result = await _stylingApi.UpdateAsync(id, request);
            if (result == null)
                return Json(new { success = false, message = "Error updating styling" });

            return Json(new { success = true, message = "Styling updated successfully", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating styling {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _stylingApi.DeleteAsync(id);
            if (!result)
                return Json(new { success = false, message = "Error deleting styling" });

            return Json(new { success = true, message = "Styling deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting styling {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ── Assignment endpoints ─────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAssignments()
    {
        try
        {
            var data = await _stylingApi.GetAssignmentsAsync();
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting styling assignments");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> AssignToBrand(int brandId, [FromBody] AssignStylingRequestDto request)
    {
        try
        {
            var ok = await _stylingApi.AssignToBrandAsync(brandId, request.StylingId);
            return Json(ok
                ? new { success = true, message = "Styling assigned to brand" }
                : new { success = false, message = "Error assigning styling to brand" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning styling to brand {BrandId}", brandId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> AssignToCategory(int categoryId, [FromBody] AssignStylingRequestDto request)
    {
        try
        {
            var ok = await _stylingApi.AssignToCategoryAsync(categoryId, request.StylingId);
            return Json(ok
                ? new { success = true, message = "Styling assigned to category" }
                : new { success = false, message = "Error assigning styling to category" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning styling to category {CategoryId}", categoryId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> AssignToModel(int modelId, [FromBody] AssignStylingRequestDto request)
    {
        try
        {
            var ok = await _stylingApi.AssignToModelAsync(modelId, request.StylingId);
            return Json(ok
                ? new { success = true, message = "Styling assigned to model" }
                : new { success = false, message = "Error assigning styling to model" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning styling to model {ModelId}", modelId);
            return Json(new { success = false, message = ex.Message });
        }
    }
}
