using Microsoft.AspNetCore.Mvc;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Company;
using Empire.Web.Authorization;

namespace Empire.Web.Controllers;

[SessionAuthorize]
public class CompanyController : Controller
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    // GET: /Company/
    public async Task<IActionResult> Index()
    {
        var companies = await _companyService.GetAllCompaniesAsync();
        return View(companies);
    }

    // POST: /Company/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _companyService.CreateCompanyAsync(request);
            return Json(new { success = true, message = "Company created successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // POST: /Company/Update
    [HttpPost]
    public async Task<IActionResult> Update([FromBody] UpdateCompanyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _companyService.UpdateCompanyAsync(request);
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
            await _companyService.DeleteCompanyAsync(id);
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
        var companies = await _companyService.GetCompanySelectionListAsync();
        return Json(companies);
    }
}
