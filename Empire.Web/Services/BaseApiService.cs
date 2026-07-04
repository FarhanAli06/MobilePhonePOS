using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Empire.Web.Constants;

namespace Empire.Web.Services;

/// <summary>
/// Base API service for making HTTP requests to backend API
/// </summary>
public abstract class BaseApiService
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;

    protected BaseApiService(
        HttpClient httpClient,
        ILogger logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Serialize/deserialize enums as camelCase strings (e.g. "phone", "unpaid") to match
            // the JsonStringEnumConverter registered on the API side (allowIntegerValues: true
            // means integer values like 0 or 1 are also accepted as fallback).
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
        };
    }

    /// <summary>
    /// Get authorization token from session
    /// </summary>
    protected string? GetAuthToken()
    {
        return _httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.AuthToken);
    }

    /// <summary>
    /// Set authorization header
    /// </summary>
    protected void SetAuthHeader()
    {
        var token = GetAuthToken();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AppConstants.Bearer, token);
        }
    }

    /// <summary>
    /// GET request
    /// </summary>
    protected async Task<T> GetAsync<T>(string endpoint) where T : class
    {
        try
        {
            _logger.LogInformation(AppConstants.LogMessages.ApiGetRequest, endpoint);
            SetAuthHeader();
            
            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                // API returns { "data": ... } via BaseApiController.SuccessResponse
                // Fall back to { "result": ... } for AutoWrapper compatibility
                return result?.Data ?? result?.Result ?? default!;
            }
            
            _logger.LogWarning(AppConstants.LogMessages.ApiRequestFailed, response.StatusCode);
            return default!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.ApiGetError, endpoint);
            throw;
        }
    }

    /// <summary>
    /// POST request
    /// </summary>
    protected async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data) 
        where TResponse : class
    {
        try
        {
            _logger.LogInformation(AppConstants.LogMessages.ApiPostRequest, endpoint);
            SetAuthHeader();
            
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, AppConstants.ContentTypeJson);
            
            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<TResponse>>(responseContent, _jsonOptions);
                return result?.Data ?? result?.Result ?? default!;
            }
            
            _logger.LogWarning(AppConstants.LogMessages.ApiRequestFailed, response.StatusCode);
            return default!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.ApiPostError, endpoint);
            throw;
        }
    }

    /// <summary>
    /// PUT request
    /// </summary>
    protected async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        where TResponse : class
    {
        try
        {
            _logger.LogInformation(AppConstants.LogMessages.ApiPutRequest, endpoint);
            SetAuthHeader();
            
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, AppConstants.ContentTypeJson);
            
            var response = await _httpClient.PutAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<TResponse>>(responseContent, _jsonOptions);
                return result?.Data ?? result?.Result ?? default!;
            }
            
            _logger.LogWarning(AppConstants.LogMessages.ApiRequestFailed, response.StatusCode);
            return default!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.ApiPutError, endpoint);
            throw;
        }
    }

    /// <summary>
    /// DELETE request
    /// </summary>
    protected async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            _logger.LogInformation(AppConstants.LogMessages.ApiDeleteRequest, endpoint);
            SetAuthHeader();
            
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.ApiDeleteError, endpoint);
            throw;
        }
    }
}

/// <summary>
/// API response wrapper.
/// Supports both "data" (BaseApiController.SuccessResponse) and "result" (AutoWrapper) JSON keys.
/// </summary>
public class ApiResponse<T> where T : class
{
    public string Version { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }

    /// <summary>Used by BaseApiController.SuccessResponse — JSON key: "data"</summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>Used by AutoWrapper — JSON key: "result" (kept for backwards compatibility)</summary>
    [JsonPropertyName("result")]
    public T? Result { get; set; }
}
