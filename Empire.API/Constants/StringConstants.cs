namespace Empire.API.Constants;

public static class StringConstants
{
    public static class ErrorMessages
    {
        public const string Unauthorized = "Unauthorized access";
        public const string NotFound = "Resource not found";
        public const string GeneralError = "An error occurred while processing your request";
        public const string ValidationError = "Validation failed";
        public const string BadRequest = "Bad request";
    }
    
    public static class SuccessMessages
    {
        public const string Created = "Resource created successfully";
        public const string Updated = "Resource updated successfully";
        public const string Deleted = "Resource deleted successfully";
        public const string Retrieved = "Resource retrieved successfully";
    }
}
