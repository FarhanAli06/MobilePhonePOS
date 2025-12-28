using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Company;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly EmpireDbContext _context;

    public CompanyService(EmpireDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync()
    {
        return await _context.Companies
            .OrderBy(c => c.Rank)
            .Select(c => new CompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                Rank = c.Rank,
                IsDefault = c.IsDefault,
                CreatedDate = c.CreatedDate,
                ModifiedDate = c.ModifiedDate
            })
            .ToListAsync();
    }

    public async Task<CompanyDto> GetCompanyByIdAsync(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null)
        {
            throw new KeyNotFoundException($"Company with ID {id} not found.");
        }

        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Rank = company.Rank,
            IsDefault = company.IsDefault,
            CreatedDate = company.CreatedDate,
            ModifiedDate = company.ModifiedDate
        };
    }

    public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request)
    {
        if (request.IsDefault)
        {
            await ClearDefaultCompanyAsync();
        }

        var company = new Company
        {
            Name = request.Name,
            Rank = request.Rank,
            IsDefault = request.IsDefault,
            CreatedDate = DateTime.UtcNow
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Rank = company.Rank,
            IsDefault = company.IsDefault,
            CreatedDate = company.CreatedDate,
            ModifiedDate = company.ModifiedDate
        };
    }

    public async Task UpdateCompanyAsync(UpdateCompanyRequest request)
    {
        var company = await _context.Companies.FindAsync(request.Id);
        if (company == null)
        {
            throw new KeyNotFoundException($"Company with ID {request.Id} not found.");
        }

        if (request.IsDefault && !company.IsDefault)
        {
            await ClearDefaultCompanyAsync();
        }

        company.Name = request.Name;
        company.Rank = request.Rank;
        company.IsDefault = request.IsDefault;
        company.ModifiedDate = DateTime.UtcNow;

        _context.Companies.Update(company);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCompanyAsync(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null)
        {
            return;
        }

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<CompanySelectionDto>> GetCompanySelectionListAsync()
    {
        return await _context.Companies
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Rank)
            .Select(c => new CompanySelectionDto
            {
                Id = c.Id,
                Name = c.Name,
                IsDefault = c.IsDefault
            })
            .ToListAsync();
    }

    private async Task ClearDefaultCompanyAsync()
    {
        var currentDefault = await _context.Companies.FirstOrDefaultAsync(c => c.IsDefault);
        if (currentDefault != null)
        {
            currentDefault.IsDefault = false;
            _context.Companies.Update(currentDefault);
            await _context.SaveChangesAsync();
        }
    }
}
