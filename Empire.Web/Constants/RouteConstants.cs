namespace Empire.Web.Constants;

/// <summary>
/// Constants for all route paths and API endpoints
/// </summary>
public static class RouteConstants
{
    /// <summary>
    /// Controller names
    /// </summary>
    public static class Controllers
    {
        public const string Home = "Home";
        public const string Auth = "Auth";
        public const string Dashboard = "Dashboard";
        public const string Repairs = "Repairs";
        public const string Devices = "Devices";
        public const string Customers = "Customers";
        public const string Inventory = "Inventory";
        public const string Sales = "Sales";
        public const string Reports = "Reports";
        public const string Settings = "Settings";
        public const string Users = "Users";
        public const string Shops = "Shops";
        public const string Brands = "Brands";
        public const string DeviceCategories = "DeviceCategories";
        public const string DeviceModels = "DeviceModels";
        public const string Lookups = "Lookups";
    }
    
    /// <summary>
    /// Action names
    /// </summary>
    public static class Actions
    {
        public const string Index = "Index";
        public const string Create = "Create";
        public const string Edit = "Edit";
        public const string Delete = "Delete";
        public const string Details = "Details";
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string Register = "Register";
        public const string Profile = "Profile";
        public const string ChangePassword = "ChangePassword";
        public const string ForgotPassword = "ForgotPassword";
        public const string ResetPassword = "ResetPassword";
    }
    
    /// <summary>
    /// API controller routes
    /// </summary>
    public static class API
    {
        public const string Auth = "api/[controller]";
        public const string Repairs = "api/[controller]";
        public const string Devices = "api/[controller]";
        public const string Customers = "api/[controller]";
        public const string Inventory = "api/[controller]";
        public const string Sales = "api/[controller]";
        public const string Lookups = "api/[controller]";
    }
    
    /// <summary>
    /// API base paths
    /// </summary>
    public static class ApiPaths
    {
        public const string Base = "/api";
        public const string Auth = "/api/auth";
        public const string Repairs = "/api/repairs";
        public const string Devices = "/api/devices";
        public const string Customers = "/api/customers";
        public const string Inventory = "/api/inventory";
        public const string Sales = "/api/sales";
        public const string Brands = "/api/brands";
        public const string DeviceCategories = "/api/devicecategories";
        public const string DeviceModels = "/api/devicemodels";
        public const string Lookups = "/api/lookups";
    }
    
    /// <summary>
    /// API endpoints
    /// </summary>
    public static class ApiEndpoints
    {
        // Auth endpoints
        public const string Login = "/api/auth/login";
        public const string Logout = "/api/auth/logout";
        public const string Register = "/api/auth/register";
        public const string RefreshToken = "/api/auth/refresh";
        
        // Repairs endpoints
        public const string GetRepairs = "/api/repairs";
        public const string GetRepairById = "/api/repairs/{0}";
        public const string GetRepairsByShop = "/api/repairs/shop/{0}";
        public const string CreateRepair = "/api/repairs";
        public const string UpdateRepair = "/api/repairs/{0}";
        public const string DeleteRepair = "/api/repairs/{0}";
        
        // Devices endpoints
        public const string GetDevices = "/api/devices";
        public const string GetDeviceById = "/api/devices/{0}";
        public const string GetDevicesByShop = "/api/devices/shop/{0}";
        public const string CreateDevice = "/api/devices";
        public const string UpdateDevice = "/api/devices/{0}";
        public const string DeleteDevice = "/api/devices/{0}";
        
        // Customers endpoints
        public const string GetCustomers = "/api/customers";
        public const string GetCustomerById = "/api/customers/{0}";
        public const string GetCustomersByShop = "/api/customers/shop/{0}";
        public const string CreateCustomer = "/api/customers";
        public const string UpdateCustomer = "/api/customers/{0}";
        public const string DeleteCustomer = "/api/customers/{0}";
        
        // Lookups endpoints
        public const string GetLookupsByCategory = "/api/lookups/category/{0}";
        public const string GetAllLookups = "/api/lookups";
        public const string CreateLookup = "/api/lookups";
        public const string UpdateLookup = "/api/lookups/{0}";
        public const string DeleteLookup = "/api/lookups/{0}";
    }
    
    /// <summary>
    /// View paths
    /// </summary>
    public static class Views
    {
        public const string Index = "Index";
        public const string Create = "Create";
        public const string Edit = "Edit";
        public const string Details = "Details";
        public const string Login = "Login";
        public const string Register = "Register";
        public const string Error = "Error";
        public const string NotFound = "NotFound";
        public const string Unauthorized = "Unauthorized";
    }
    
    /// <summary>
    /// Route templates
    /// </summary>
    public static class Templates
    {
        public const string Default = "{controller=Home}/{action=Index}/{id?}";
        public const string Api = "api/[controller]";
        public const string ApiWithAction = "api/[controller]/[action]";
    }
}
