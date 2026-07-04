using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Empire.Infrastructure.Data;
using Empire.Domain.Entities;

namespace Empire.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : BaseApiController
    {
        private readonly EmpireDbContext _context;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(EmpireDbContext context, ILogger<CompaniesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>Get all companies.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var companies = await _context.Companies
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.Rank)
                    .ThenBy(c => c.Name)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Rank,
                        c.IsDefault,
                        CreatedDate = c.CreatedDateUtc,
                        ModifiedDate = c.ModifiedDateUtc
                    })
                    .ToListAsync();
                return SuccessResponse(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving companies");
                return StatusCode(500, "An error occurred while retrieving companies");
            }
        }

        /// <summary>Get active companies.</summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            try
            {
                var companies = await _context.Companies
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.Rank)
                    .ThenBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name, c.IsDefault })
                    .ToListAsync();
                return SuccessResponse(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active companies");
                return StatusCode(500, "An error occurred while retrieving active companies");
            }
        }

        /// <summary>Get company selection list.</summary>
        [HttpGet("selections")]
        public async Task<IActionResult> GetSelections()
        {
            try
            {
                var companies = await _context.Companies
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.Rank)
                    .ThenBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name, c.IsDefault })
                    .ToListAsync();
                return SuccessResponse(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company selections");
                return StatusCode(500, "An error occurred while retrieving company selections");
            }
        }

        /// <summary>Get a single company by ID.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var company = await _context.Companies
                    .Where(c => c.Id == id && !c.IsDeleted)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Rank,
                        c.IsDefault,
                        CreatedDate = c.CreatedDateUtc,
                        ModifiedDate = c.ModifiedDateUtc
                    })
                    .FirstOrDefaultAsync();
                if (company == null)
                    return NotFoundResponse("Company not found");
                return SuccessResponse(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the company");
            }
        }

        /// <summary>Check for duplicate company name.</summary>
        [HttpGet("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] int? excludeId = null)
        {
            var query = _context.Companies.Where(c => c.Name == name && !c.IsDeleted);
            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);
            var isDuplicate = await query.AnyAsync();
            return SuccessResponse(isDuplicate);
        }

        /// <summary>Create a new company.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompanyApiRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid company data");
            try
            {
                // If this is set as default, clear existing defaults
                if (request.IsDefault)
                {
                    var existingDefaults = await _context.Companies.Where(c => c.IsDefault && !c.IsDeleted).ToListAsync();
                    foreach (var d in existingDefaults) d.IsDefault = false;
                }
                var company = new Company
                {
                    Name = request.Name,
                    Rank = request.Rank,
                    IsDefault = request.IsDefault,
                    CreatedDateUtc = DateTime.UtcNow,
                    ModifiedDateUtc = DateTime.UtcNow
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Company {Name} created", company.Name);
                return SuccessResponse(new { company.Id, company.Name, company.Rank, company.IsDefault, CreatedDate = company.CreatedDateUtc }, "Company created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return StatusCode(500, "An error occurred while creating the company");
            }
        }

        /// <summary>Update an existing company.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyApiRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid company data");
            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                if (company == null)
                    return NotFoundResponse("Company not found");
                // If this is set as default, clear existing defaults
                if (request.IsDefault && !company.IsDefault)
                {
                    var existingDefaults = await _context.Companies.Where(c => c.IsDefault && !c.IsDeleted && c.Id != id).ToListAsync();
                    foreach (var d in existingDefaults) d.IsDefault = false;
                }
                company.Name = request.Name;
                company.Rank = request.Rank;
                company.IsDefault = request.IsDefault;
                company.ModifiedDateUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Company {Id} updated", id);
                return SuccessResponse(new { company.Id, company.Name, company.Rank, company.IsDefault, ModifiedDate = company.ModifiedDateUtc }, "Company updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {Id}", id);
                return StatusCode(500, "An error occurred while updating the company");
            }
        }

        /// <summary>Delete a company (soft delete).</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                if (company == null)
                    return NotFoundResponse("Company not found");
                company.IsDeleted = true;
                company.ModifiedDateUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Company {Id} deleted", id);
                return SuccessResponse("Company deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company {Id}", id);
                return StatusCode(500, "An error occurred while deleting the company");
            }
        }
    }

    public class CreateCompanyApiRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Rank { get; set; }
        public bool IsDefault { get; set; }
    }

    public class UpdateCompanyApiRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Rank { get; set; }
        public bool IsDefault { get; set; }
    }
}
