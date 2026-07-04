using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for database configuration and initialization
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Add database context and related services to the service collection
        /// </summary>
        public static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            IConfiguration configuration,
            string migrationsAssembly = "Empire.Infrastructure")
        {
            // Register DbContext
            services.AddDbContext<EmpireDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection") ??
                    "Server=(localdb)\\MSSQLLocalDB;Database=EmpireSolutionDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
                    b => b.MigrationsAssembly(migrationsAssembly).EnableRetryOnFailure()
                ));

            // Register database initializer
            services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

            return services;
        }

        /// <summary>
        /// Initialize the database by applying migrations and seeding data
        /// </summary>
        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            await initializer.InitializeAsync();
        }
    }
}
