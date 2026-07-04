namespace Empire.Web.Constants;

/// <summary>
/// Constants for page titles, headings, and UI text
/// </summary>
public static class PageConstants
{
    /// <summary>
    /// Page titles
    /// </summary>
    public static class Titles
    {
        public const string Dashboard = "Dashboard";
        public const string Repairs = "Repairs";
        public const string CreateRepair = "Create Repair";
        public const string EditRepair = "Edit Repair";
        public const string RepairDetails = "Repair Details";
        
        public const string Devices = "Devices";
        public const string CreateDevice = "Create Device";
        public const string EditDevice = "Edit Device";
        public const string DeviceDetails = "Device Details";
        
        public const string Customers = "Customers";
        public const string CreateCustomer = "Create Customer";
        public const string EditCustomer = "Edit Customer";
        public const string CustomerDetails = "Customer Details";
        
        public const string Inventory = "Inventory";
        public const string CreateInventoryItem = "Create Inventory Item";
        public const string EditInventoryItem = "Edit Inventory Item";
        public const string InventoryDetails = "Inventory Details";
        
        public const string Sales = "Sales";
        public const string CreateSale = "Create Sale";
        public const string SaleDetails = "Sale Details";
        
        public const string Reports = "Reports";
        public const string Settings = "Settings";
        public const string Profile = "Profile";
        public const string Login = "Login";
        public const string Register = "Register";
        public const string ForgotPassword = "Forgot Password";
        public const string ResetPassword = "Reset Password";
        public const string ChangePassword = "Change Password";
    }
    
    /// <summary>
    /// Page headings
    /// </summary>
    public static class Headings
    {
        public const string RepairManagement = "Repair Management";
        public const string DeviceManagement = "Device Management";
        public const string CustomerManagement = "Customer Management";
        public const string InventoryManagement = "Inventory Management";
        public const string SalesManagement = "Sales Management";
        public const string UserManagement = "User Management";
        public const string ShopManagement = "Shop Management";
        public const string SystemSettings = "System Settings";
    }
    
    /// <summary>
    /// Button text
    /// </summary>
    public static class Buttons
    {
        public const string Save = "Save";
        public const string Cancel = "Cancel";
        public const string Delete = "Delete";
        public const string Edit = "Edit";
        public const string Create = "Create";
        public const string Submit = "Submit";
        public const string Back = "Back";
        public const string Search = "Search";
        public const string Filter = "Filter";
        public const string Reset = "Reset";
        public const string Export = "Export";
        public const string Import = "Import";
        public const string Print = "Print";
        public const string Download = "Download";
        public const string Upload = "Upload";
        public const string Close = "Close";
        public const string Confirm = "Confirm";
        public const string Yes = "Yes";
        public const string No = "No";
        public const string Ok = "OK";
        public const string Apply = "Apply";
        public const string Clear = "Clear";
        public const string Refresh = "Refresh";
        public const string Add = "Add";
        public const string Remove = "Remove";
        public const string Update = "Update";
        public const string View = "View";
        public const string Details = "Details";
    }
    
    /// <summary>
    /// Labels
    /// </summary>
    public static class Labels
    {
        public const string Name = "Name";
        public const string Description = "Description";
        public const string Status = "Status";
        public const string Date = "Date";
        public const string Time = "Time";
        public const string Price = "Price";
        public const string Quantity = "Quantity";
        public const string Total = "Total";
        public const string Subtotal = "Subtotal";
        public const string Tax = "Tax";
        public const string Discount = "Discount";
        public const string GrandTotal = "Grand Total";
        public const string Email = "Email";
        public const string Phone = "Phone";
        public const string Address = "Address";
        public const string City = "City";
        public const string State = "State";
        public const string ZipCode = "Zip Code";
        public const string Country = "Country";
        public const string Notes = "Notes";
        public const string Comments = "Comments";
        public const string Actions = "Actions";
        public const string CreatedDate = "Created Date";
        public const string ModifiedDate = "Modified Date";
        public const string CreatedBy = "Created By";
        public const string ModifiedBy = "Modified By";
    }
    
    /// <summary>
    /// Messages
    /// </summary>
    public static class Messages
    {
        public const string Loading = "Loading...";
        public const string Saving = "Saving...";
        public const string Processing = "Processing...";
        public const string PleaseWait = "Please wait...";
        public const string NoDataAvailable = "No data available";
        public const string NoRecordsFound = "No records found";
        public const string SelectOption = "-- Select an option --";
        public const string SelectAll = "Select All";
        public const string DeselectAll = "Deselect All";
    }
    
    /// <summary>
    /// Confirmation messages
    /// </summary>
    public static class Confirmations
    {
        public const string DeleteConfirm = "Are you sure you want to delete this item?";
        public const string SaveConfirm = "Are you sure you want to save these changes?";
        public const string CancelConfirm = "Are you sure you want to cancel? Unsaved changes will be lost.";
        public const string LogoutConfirm = "Are you sure you want to logout?";
    }
    
    /// <summary>
    /// Repair-specific page messages
    /// </summary>
    public static class Repair
    {
        public const string RepairUpdatedSuccess = "Repair updated successfully.";
        public const string RepairNotFound = "Repair not found.";
        public const string RepairDeletedSuccess = "Repair deleted successfully.";
        public const string RepairCreatedSuccess = "Repair created successfully.";
        public const string NotAvailable = "Not Available";
        public const string ErrorLoadingRepairs = "Error loading repairs.";
        public const string ErrorLoadingPage = "Error loading page.";
        public const string ErrorDeletingRepair = "Error deleting repair.";
    }
    
    /// <summary>
    /// Device-specific page messages
    /// </summary>
    public static class Device
    {
        public const string Title = "Device Management";
        public const string DeviceCreatedSuccess = "Device created successfully.";
        public const string DeviceUpdatedSuccess = "Device updated successfully.";
        public const string DeviceDeletedSuccess = "Device deleted successfully.";
        public const string StatusUpdatedSuccess = "Device status updated successfully.";
        public const string DeviceMarkedAsSold = "Device marked as sold successfully.";
        public const string DeviceNotFound = "Device not found.";
        public const string ErrorLoadingDevices = "Error loading devices.";
        public const string ErrorLoadingPage = "Error loading device page.";
    }
    
    /// <summary>
    /// Authentication-specific page messages
    /// </summary>
    public static class Auth
    {
        public const string LogoutSuccess = "Logged out successfully.";
        public const string LoginSuccess = "Logged in successfully.";
    }
}
