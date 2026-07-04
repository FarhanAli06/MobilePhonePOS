namespace Empire.Web.Services.Http;

/// <summary>
/// Generic HTTP client service interface for API communication
/// </summary>
public interface IHttpClientService
{
    /// <summary>
    /// Send GET request
    /// </summary>
    Task<T?> GetAsync<T>(string endpoint);

    /// <summary>
    /// Send POST request
    /// </summary>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);

    /// <summary>
    /// Send PUT request
    /// </summary>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data);

    /// <summary>
    /// Send DELETE request
    /// </summary>
    Task<bool> DeleteAsync(string endpoint);

    /// <summary>
    /// Send GET request with an explicit Bearer token (used when the token is not yet
    /// stored in session/cookie, e.g. immediately after login).
    /// </summary>
    Task<T?> GetWithTokenAsync<T>(string endpoint, string bearerToken);

    /// <summary>
    /// Send GET request and return raw response
    /// </summary>
    Task<HttpResponseMessage> GetRawAsync(string endpoint);

    /// <summary>
    /// Send POST request and return raw response
    /// </summary>
    Task<HttpResponseMessage> PostRawAsync<TRequest>(string endpoint, TRequest data);
}
