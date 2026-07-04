namespace Empire.Web.Constants;

/// <summary>
/// Application-wide constants for common values
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Empty string constant - use instead of ""
    /// </summary>
    public const string EmptyString = "";
    
    /// <summary>
    /// Default page size for pagination
    /// </summary>
    public const int DefaultPageSize = 20;
    
    /// <summary>
    /// Maximum page size for pagination
    /// </summary>
    public const int MaxPageSize = 100;
    
    /// <summary>
    /// Default currency symbol
    /// </summary>
    public const string CurrencySymbol = "$";
    
    /// <summary>
    /// Default date format
    /// </summary>
    public const string DateFormat = "yyyy-MM-dd";
    
    /// <summary>
    /// Default datetime format
    /// </summary>
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    
    /// <summary>
    /// Default time format
    /// </summary>
    public const string TimeFormat = "HH:mm";
    
    /// <summary>
    /// Default decimal format for prices
    /// </summary>
    public const string PriceFormat = "0.00";
    
    /// <summary>
    /// Default decimal format for percentages
    /// </summary>
    public const string PercentageFormat = "0.00";
    
    /// <summary>
    /// Not Available text
    /// </summary>
    public const string NotAvailable = "N/A";
    
    /// <summary>
    /// Unknown text
    /// </summary>
    public const string Unknown = "Unknown";
    
    /// <summary>
    /// Default timeout for API calls (in seconds)
    /// </summary>
    public const int DefaultApiTimeout = 30;
    
    /// <summary>
    /// Session timeout (in minutes)
    /// </summary>
    public const int SessionTimeout = 60;
    
    /// <summary>
    /// Bearer token prefix for authorization
    /// </summary>
    public const string Bearer = "Bearer";
    
    /// <summary>
    /// Content type for JSON
    /// </summary>
    public const string ContentTypeJson = "application/json";
    
    /// <summary>
    /// Log messages for API calls
    /// </summary>
    public static class LogMessages
    {
        public const string ApiGetRequest = "GET request to {Endpoint}";
        public const string ApiPostRequest = "POST request to {Endpoint}";
        public const string ApiPutRequest = "PUT request to {Endpoint}";
        public const string ApiDeleteRequest = "DELETE request to {Endpoint}";
        public const string ApiRequestFailed = "API request failed: {StatusCode}";
        public const string ApiGetError = "Error in GET request to {Endpoint}";
        public const string ApiPostError = "Error in POST request to {Endpoint}";
        public const string ApiPutError = "Error in PUT request to {Endpoint}";
        public const string ApiDeleteError = "Error in DELETE request to {Endpoint}";
    }
}
