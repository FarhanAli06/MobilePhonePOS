using Empire.Web.Services;
using Empire.Web.Services.Http;
using Empire.Web.Services.API;
using Empire.Web.Services.Repair;
using Empire.Web.Services.POS;
using Empire.Web.Services.Customer;
using Empire.Web.Services.Inventory;
using Empire.Web.Services.Auth;
using Empire.Web.Services.Device;
using Empire.Web.Services.Lookup;
using Empire.Web.Services.Brand;
using Empire.Web.Services.DeviceCategory;
using Empire.Web.Services.DeviceModel;
using Empire.Web.Services.Item;
using Empire.Web.Services.User;
using Empire.Web.Services.Shop;
using Empire.Web.Services.InventoryTransaction;
using Empire.Web.Services.Home;
using Empire.Web.Services.Dashboard;
using Empire.Web.Services.Helper;
using Empire.Web.Services.Session;
using Empire.Web.Services.Mapping;
using Empire.Web.Services.POSMapping;
using Empire.Web.Services.Category;
using Empire.Web.Services.LookupValue;
using Empire.Web.Services.Company;
using Empire.Web.Services.Sales;
using Empire.Web.Services.InventoryManagement;
using Empire.Web.Services.Role;
using Empire.Web.Services.LookupManagement;
using Empire.Web.Services.Log;
using Empire.Web.Services.Styling;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Register HttpContextAccessor for timezone service
builder.Services.AddHttpContextAccessor();

// Register timezone service
builder.Services.AddScoped<ITimezoneService, TimezoneService>();


//Comment by farhan

//// Register database services and migrations (moved to Infrastructure layer)
//builder.Services.AddDatabaseServices(builder.Configuration, migrationsAssembly: "Empire.Infrastructure");

//// Register password hash service (required for database initialization)
//builder.Services.AddScoped<Empire.Application.Interfaces.IPasswordHashService, Empire.Infrastructure.Services.PasswordHashService>();




// Register Token Storage Service
builder.Services.AddScoped<Empire.Web.Services.Auth.ITokenStorageService, Empire.Web.Services.Auth.TokenStorageService>();

// Register Authentication Handler
builder.Services.AddTransient<Empire.Web.Handlers.AuthenticationDelegatingHandler>();

// ── Shared HttpClient configuration helper ────────────────────────────────────
// Used for services that inject a raw HttpClient (BaseApiService subclasses).
// AddHttpClient is required so that ASP.NET Core's IHttpClientFactory sets
// BaseAddress; AddScoped alone leaves BaseAddress null and causes:
//   "An invalid request URI was provided. Either the request URI must be an
//    absolute URI or BaseAddress must be set."
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException(
        "ApiSettings:BaseUrl is not configured. " +
        "Add it to appsettings.json: \"ApiSettings\": { \"BaseUrl\": \"http://your-api-host\" }");
// Ensure the base URL always ends with a trailing slash (required by HttpClient)
if (!apiBaseUrl.EndsWith("/", StringComparison.Ordinal)) apiBaseUrl += "/";

void ConfigureApiHttpClient(HttpClient client)
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}

// ── IHttpClientService (generic wrapper used by most services) ────────────────
builder.Services.AddHttpClient<IHttpClientService, HttpClientService>(ConfigureApiHttpClient)
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    // Bypass SSL validation in development to avoid UntrustedRoot errors with the dev cert
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
})
.AddHttpMessageHandler<Empire.Web.Handlers.AuthenticationDelegatingHandler>();

// ── Services that extend BaseApiService (inject raw HttpClient) ───────────────
// These MUST use AddHttpClient so BaseAddress is populated by IHttpClientFactory.
builder.Services.AddHttpClient<IUserApiService, UserApiService>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<Empire.Web.Handlers.AuthenticationDelegatingHandler>();
builder.Services.AddHttpClient<IRoleApiService, RoleApiService>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<Empire.Web.Handlers.AuthenticationDelegatingHandler>();
builder.Services.AddHttpClient<ICustomerApiService, CustomerApiService>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<Empire.Web.Handlers.AuthenticationDelegatingHandler>();
builder.Services.AddHttpClient<IRepairApiService, RepairApiService>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<Empire.Web.Handlers.AuthenticationDelegatingHandler>();
builder.Services.AddHttpClient<IDeviceApiService, DeviceApiService>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<Empire.Web.Handlers.AuthenticationDelegatingHandler>();
builder.Services.AddHttpClient<ILookupApiService, LookupApiService>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<Empire.Web.Handlers.AuthenticationDelegatingHandler>();
builder.Services.AddScoped<IItemApiService, ItemApiService>();
builder.Services.AddScoped<ICompanyApiService, CompanyApiService>();

// ── Services that inject IHttpClientService (already has BaseAddress) ─────────
// AddScoped is correct here — IHttpClientService is resolved from the
// IHttpClientFactory-managed typed client registered above.
builder.Services.AddScoped<IShopApiService, ShopApiService>();
builder.Services.AddScoped<IInventoryApiService, InventoryApiService>();
builder.Services.AddScoped<IAuthApiService, AuthApiService>();
builder.Services.AddScoped<IBrandApiService, BrandApiService>();
builder.Services.AddScoped<IDeviceCategoryApiService, DeviceCategoryApiService>();
builder.Services.AddScoped<IDeviceModelApiService, DeviceModelApiService>();
builder.Services.AddScoped<IPOSApiService, POSApiService>();
builder.Services.AddScoped<IInventoryTransactionApiService, InventoryTransactionApiService>();
builder.Services.AddScoped<Empire.Web.Services.API.IInventoryCategoryApiService, Empire.Web.Services.API.InventoryCategoryApiService>();
builder.Services.AddScoped<ICategoryApiService, CategoryApiService>();
builder.Services.AddScoped<ILookupValueApiService, LookupValueApiService>();
builder.Services.AddScoped<ISalesApiService, SalesApiService>();
builder.Services.AddScoped<IInventoryManagementApiService, InventoryManagementApiService>();
builder.Services.AddScoped<Empire.Web.Services.PagePermission.IPagePermissionApiService, Empire.Web.Services.PagePermission.PagePermissionApiService>();
builder.Services.AddScoped<ILookupManagementService, LookupManagementService>();
builder.Services.AddScoped<ILogApiService, LogApiService>();
builder.Services.AddScoped<IStylingApiService, StylingApiService>();

// Register helper services
builder.Services.AddScoped<IHelperService, HelperService>();
builder.Services.AddScoped<ISessionHelper, SessionHelper>();
builder.Services.AddScoped<IMappingHelper, MappingHelper>();
builder.Services.AddScoped<IPOSMappingService, POSMappingService>();
builder.Services.AddScoped<Empire.Web.Services.ViewData.IViewDataService, Empire.Web.Services.ViewData.ViewDataService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Session - required for SessionAuthorize attribute and Layout user display
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
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

// Session middleware - must be after UseRouting and before MapControllers
app.UseSession();

// Configure routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// API routes
app.MapControllers();


//Comment by farhan

// Initialize database (migrations and seeding) - handled by Infrastructure layer
//await app.Services.InitializeDatabaseAsync();

app.Run();
