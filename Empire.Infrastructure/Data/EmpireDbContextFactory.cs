using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Empire.Infrastructure.Data;

/// <summary>
/// Design-time factory used exclusively by EF Core CLI tools
/// (dotnet ef migrations add / database update) when running from the
/// Empire.Infrastructure project.  It reads the connection string from
/// Empire.API/appsettings.json so the same database is targeted as at
/// runtime, without requiring the full DI container to be built.
/// </summary>
public class EmpireDbContextFactory : IDesignTimeDbContextFactory<EmpireDbContext>
{
    public EmpireDbContext CreateDbContext(string[] args)
    {
        // Walk up from the Infrastructure project directory to find the
        // API project's appsettings.json at design time.
        var basePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "Empire.API");

        // Fall back to the current directory if the API project is not found
        // (e.g. when running from the solution root).
        if (!Directory.Exists(basePath))
            basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=EmpireSolutionDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        var optionsBuilder = new DbContextOptionsBuilder<EmpireDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        // IHttpContextAccessor is not available at design time; the
        // EmpireDbContext constructor accepts it as optional (nullable).
        return new EmpireDbContext(optionsBuilder.Options, httpContextAccessor: null);
    }
}
