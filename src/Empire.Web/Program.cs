using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Empire.Infrastructure.Data;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Repositories;
using Empire.Application.Interfaces;
using Empire.Infrastructure.Services;
using Empire.Domain.Entities;
using Empire.Domain.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure Entity Framework with SQL Server only
builder.Services.AddDbContext<EmpireDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                       "Server=(localdb)\\mssqllocaldb;Database=EmpireSolutionDb;Trusted_Connection=true;MultipleActiveResultSets=true",
                       b => b.MigrationsAssembly("Empire.Web").EnableRetryOnFailure()));

// Register repositories
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRepairRepository, RepairRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

// Register services
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRepairService, RepairService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IUserService, UserService>();

// Configure Session-based Authentication only
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Home/Login";
    options.LogoutPath = "/Home/Logout";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Match session timeout
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Configure routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// API routes
app.MapControllers();

// Ensure database is created and seed super admin
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EmpireDbContext>();
    var passwordHashService = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
    
    try
    {
        // Apply migrations
        context.Database.Migrate();
        
        // Create super admin user if not exists
        if (!context.Users.Any(u => u.Username == "superadmin"))
        {
            var superAdmin = new User
            {
                Username = "superadmin",
                Email = "superadmin@empiresolution.com",
                FirstName = "Super",
                LastName = "Admin",
                PasswordHash = passwordHashService.HashPassword("admin123"),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            
            context.Users.Add(superAdmin);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database: {Message}", ex.Message);
    }
}

app.Run();

