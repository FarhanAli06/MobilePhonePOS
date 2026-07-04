using Microsoft.AspNetCore.Mvc;
using Empire.Web.Authorization;
using Empire.Web.DTOs.Company;
using Empire.Web.Services.Company;

namespace Empire.Web.Controllers;

[SessionAuthorize]
public class CompanyController : Controller
{
    private readonly ICompanyApiService _companyService;

    public CompanyController(ICompanyApiService companyService)
    {
        _companyService = companyService;
    }

    // GET: /Company/
    public async Task<IActionResult> Index()
    {
        var companies = await _companyService.GetAllAsync();
        return View(companies);
    }

    // POST: /Company/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _companyService.CreateAsync(request);
            return Json(new { success = true, message = "Company created successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // POST: /Company/Update
    [HttpPost]
    public async Task<IActionResult> Update([FromBody] UpdateCompanyRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _companyService.UpdateAsync(request);
            return Json(new { success = true, message = "Company updated successfully." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "Company not found." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // POST: /Company/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _companyService.DeleteAsync(id);
            return Json(new { success = true, message = "Company deleted successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    // GET: /Company/GetSelectionList
    [HttpGet]
    public async Task<IActionResult> GetSelectionList()
    {
        var companies = await _companyService.GetSelectionListAsync();
        return Json(companies);
    }
}
