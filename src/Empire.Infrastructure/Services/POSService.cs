using Empire.Application.Interfaces;
using Empire.Application.DTOs.Sale;
using Empire.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Empire.Infrastructure.Services;

public class POSService : IPOSService
{
    private readonly EmpireDbContext _context;
    private readonly ILogger<POSService> _logger;

    public POSService(
        EmpireDbContext context,
        ILogger<POSService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Add POS service methods implementation as needed
}
