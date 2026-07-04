namespace Empire.Web.Constants;

/// <summary>
/// Constants for validation and error messages
/// </summary>
public static class ValidationMessages
{
    /// <summary>
    /// Required field messages
    /// </summary>
    public static class Required
    {
        public const string Field = "{0} is required.";
        public const string Name = "Name is required.";
        public const string Email = "Email is required.";
        public const string Phone = "Phone is required.";
        public const string Password = "Password is required.";
        public const string Username = "Username is required.";
        public const string Description = "Description is required.";
        public const string Price = "Price is required.";
        public const string Quantity = "Quantity is required.";
        public const string Date = "Date is required.";
        public const string Status = "Status is required.";
        public const string Customer = "Customer is required.";
        public const string Device = "Device is required.";
        public const string Brand = "Brand is required.";
        public const string Model = "Model is required.";
        public const string Category = "Category is required.";
    }
    
    /// <summary>
    /// Format validation messages
    /// </summary>
    public static class Format
    {
        public const string Email = "Please enter a valid email address.";
        public const string Phone = "Please enter a valid phone number.";
        public const string Date = "Please enter a valid date.";
        public const string Number = "Please enter a valid number.";
        public const string Decimal = "Please enter a valid decimal number.";
        public const string Url = "Please enter a valid URL.";
        public const string ZipCode = "Please enter a valid zip code.";
    }
    
    /// <summary>
    /// Range validation messages
    /// </summary>
    public static class Range
    {
        public const string Between = "{0} must be between {1} and {2}.";
        public const string Minimum = "{0} must be at least {1}.";
        public const string Maximum = "{0} must not exceed {1}.";
        public const string PositiveNumber = "{0} must be a positive number.";
        public const string GreaterThanZero = "{0} must be greater than zero.";
    }
    
    /// <summary>
    /// Length validation messages
    /// </summary>
    public static class Length
    {
        public const string MinLength = "{0} must be at least {1} characters.";
        public const string MaxLength = "{0} must not exceed {1} characters.";
        public const string ExactLength = "{0} must be exactly {1} characters.";
        public const string BetweenLength = "{0} must be between {1} and {2} characters.";
    }
    
    /// <summary>
    /// Comparison validation messages
    /// </summary>
    public static class Comparison
    {
        public const string PasswordMismatch = "Password and confirmation password do not match.";
        public const string MustMatch = "{0} and {1} must match.";
        public const string MustBeDifferent = "{0} and {1} must be different.";
        public const string EndDateAfterStartDate = "End date must be after start date.";
    }
    
    /// <summary>
    /// Business rule validation messages
    /// </summary>
    public static class BusinessRules
    {
        public const string DuplicateEntry = "A record with this {0} already exists.";
        public const string RecordNotFound = "The requested {0} was not found.";
        public const string CannotDelete = "Cannot delete {0} because it is being used.";
        public const string InsufficientStock = "Insufficient stock available.";
        public const string InvalidOperation = "This operation is not allowed.";
        public const string Unauthorized = "You are not authorized to perform this action.";
        public const string SessionExpired = "Your session has expired. Please login again.";
        public const string InvalidCredentials = "Invalid username or password.";
    }
    
    /// <summary>
    /// Success messages
    /// </summary>
    public static class Success
    {
        public const string Created = "{0} created successfully.";
        public const string Updated = "{0} updated successfully.";
        public const string Deleted = "{0} deleted successfully.";
        public const string Saved = "Changes saved successfully.";
        public const string OperationCompleted = "Operation completed successfully.";
    }
    
    /// <summary>
    /// Error messages
    /// </summary>
    public static class Errors
    {
        public const string Generic = "An error occurred. Please try again.";
        public const string CreateFailed = "Failed to create {0}. Please try again.";
        public const string UpdateFailed = "Failed to update {0}. Please try again.";
        public const string DeleteFailed = "Failed to delete {0}. Please try again.";
        public const string LoadFailed = "Failed to load {0}. Please try again.";
        public const string SaveFailed = "Failed to save changes. Please try again.";
        public const string NetworkError = "Network error. Please check your connection.";
        public const string ServerError = "Server error. Please contact support.";
        public const string ValidationFailed = "Validation failed. Please check the form.";
    }
    
    /// <summary>
    /// Warning messages
    /// </summary>
    public static class Warnings
    {
        public const string UnsavedChanges = "You have unsaved changes.";
        public const string LowStock = "Stock is running low for this item.";
        public const string OutOfStock = "This item is out of stock.";
        public const string ExpiringSoon = "This item is expiring soon.";
        public const string Overdue = "This item is overdue.";
    }
    
    /// <summary>
    /// Repair-specific validation messages
    /// </summary>
    public static class Repair
    {
        public const string OnlyManagersCanDelete = "Only managers can delete repairs.";
        public const string FailedToRetrieveDeviceModels = "Failed to retrieve device models.";
        public const string FailedToRetrieveDeviceCategories = "Failed to retrieve device categories.";
        public const string FailedToRetrieveCustomers = "Failed to retrieve customers.";
        public const string FailedToRetrieveBrands = "Failed to retrieve brands.";
        public const string FailedToRetrieveInventoryParts = "Failed to retrieve inventory parts.";
        public const string ErrorUpdatingRepair = "Error updating repair.";
        public const string ErrorCreatingRepair = "Error creating repair.";
    }
    
    /// <summary>
    /// Device-specific validation messages
    /// </summary>
    public static class Device
    {
        public const string ErrorCreatingDevice = "Failed to create device.";
        public const string ErrorUpdatingDevice = "Failed to update device.";
        public const string ErrorDeletingDevice = "Failed to delete device.";
        public const string ErrorUpdatingStatus = "Failed to update device status.";
        public const string ErrorMarkingAsSold = "Failed to mark device as sold.";
    }
    
    /// <summary>
    /// Authentication-specific validation messages
    /// </summary>
    public static class Auth
    {
        public const string InvalidCredentials = "Invalid username or password.";
        public const string InvalidRefreshToken = "Invalid refresh token.";
        public const string UserNotFound = "User not found.";
        public const string Unauthorized = "You are not authorized to perform this action.";
    }
}
