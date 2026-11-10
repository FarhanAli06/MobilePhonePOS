using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Empire.Infrastructure.Data;
using Empire.Infrastructure.Extensions;
using Empire.API.Services;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Repositories;
using Empire.Application.Interfaces;
using Empire.Application.Services;
using Empire.Infrastructure.Services;
using Empire.API.Middleware;
// using Empire.Web.Services; // TimezoneService moved to Infrastructure

var builder = WebApplication.CreateBuilder(args);

// The desktop POS runs as a normal Windows user. The default Windows Event Log
// provider can require elevated access and previously caused startup to abort
// while reporting an otherwise harmless EF warning. Console/debug logging keeps
// diagnostics available without that protected system dependency.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddSingleton<DatabaseBackupState>();
builder.Services.AddHostedService<DatabaseBackupService>();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use camelCase to match the Web client (BaseApiService serializes with CamelCase)
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Compact JSON reduces payload size and parsing work throughout the POS.
        options.JsonSerializerOptions.WriteIndented = false;
        // Accept both string ("Unpaid") and integer (1) enum values from Web clients
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(
                System.Text.Json.JsonNamingPolicy.CamelCase, allowIntegerValues: true));
    });

builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Empire POS API",
        Version = "v1",
        Description = "RESTful API for Empire Point of Sale System",
        Contact = new OpenApiContact
        {
            Name = "Empire Solutions",
            Email = "support@empiresolution.com"
        }
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure database services (moved to Infrastructure layer)
builder.Services.AddDatabaseServices(builder.Configuration, migrationsAssembly: "Empire.Infrastructure");

// Register repositories
builder.Services.AddScoped<IRequestLogRepository, RequestLogRepository>();
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRepairRepository, RepairRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

// Register HttpContextAccessor for timezone service
builder.Services.AddHttpContextAccessor();

// Register timezone service (optional - implement if needed in API)
// builder.Services.AddScoped<ITimezoneService, TimezoneService>();

// Register application services
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRepairService, RepairService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IInventoryItemService, InventoryItemService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPOSService, POSService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<InventoryTransactionService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IRequestLogService, RequestLogService>();
builder.Services.AddScoped<IPagePermissionService, PagePermissionService>();

// Configure JWT Settings
builder.Services.Configure<Empire.Infrastructure.Configuration.JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IJwtTokenService, Empire.Infrastructure.Services.Auth.JwtTokenService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EmpireAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EmpireWeb";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };

    // Handle authentication events
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

//// Configure CORS
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowWeb", policy =>
//    {
//        policy.SetIsOriginAllowed(origin =>
//            {
//                // Allow any localhost origin regardless of port
//                var uri = new Uri(origin);
//                return uri.Host == "localhost" || uri.Host == "127.0.0.1";
//            })
//            .AllowAnyMethod()
//            .AllowAnyHeader()
//            .AllowCredentials()
//            .WithExposedHeaders("Token-Expired"); // Expose custom headers
//    });
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // Allows all domains
              .AllowAnyMethod()   // Allows GET, POST, PUT, DELETE, etc.
              .AllowAnyHeader();  // Allows all request headers
    });
});
// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Empire POS API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger at root
    });
}

// Note: UseHttpsRedirection is intentionally omitted.
// The API is configured to listen on HTTP in development (http://localhost:5002).
// Enabling HTTPS redirection would cause 301 redirects that break the Web→API
// HttpClient calls and result in SSL certificate errors.
// Re-enable this only when deploying to production with a valid TLS certificate.
// app.UseHttpsRedirection();

// Global exception handler - must be first
app.UseGlobalExceptionHandler();

// Enable CORS
app.UseCors("AllowWeb");

// Log every incoming request and outgoing response (after CORS, before auth)
app.UseRequestLogging();

// Response wrapper - after CORS, before authentication
app.UseResponseWrapper();

// JWT Authentication Middleware (custom)
app.UseMiddleware<JwtAuthenticationMiddleware>();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var error = new
        {
            success = false,
            message = "An internal server error occurred.",
            timestamp = DateTime.UtcNow
        };
        
        await context.Response.WriteAsJsonAsync(error);
    });
});

// Initialize database (migrations and seeding) - handled by Infrastructure layer
await app.Services.InitializeDatabaseAsync();

app.Run();
