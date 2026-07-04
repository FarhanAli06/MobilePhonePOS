using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Empire.Web.Constants;

namespace Empire.Web.Services.Http;

/// <summary>
/// Generic HTTP client service implementation for API communication.
/// Every outgoing call is timed and logged to the console/file sink.
/// Structured log entries include method, endpoint, status code, elapsed time,
/// and — on failures — the response body for diagnostics.
/// </summary>
public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpClientService(
        HttpClient httpClient,
        ILogger<HttpClientService> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        var apiBaseUrl = configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "ApiSettings:BaseUrl is not configured. Add it to appsettings.json: " +
                "\"ApiSettings\": { \"BaseUrl\": \"http://your-api-host\" }");
        // HttpClient BaseAddress must end with a trailing slash
        if (!apiBaseUrl.EndsWith("/", StringComparison.Ordinal)) apiBaseUrl += "/";
        _httpClient.BaseAddress = new Uri(apiBaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // ─── Auth header helper ───────────────────────────────────────────────────

    private void SetAuthHeader()
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.AuthToken);
        if (!string.IsNullOrEmpty(token))
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    // ─── Envelope unwrapper ───────────────────────────────────────────────────

    private static string UnwrapEnvelope(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try
        {
            var node = JsonNode.Parse(json);
            if (node is JsonObject obj && obj.ContainsKey("success") && obj.ContainsKey("data"))
                return obj["data"]?.ToJsonString() ?? "null";
        }
        catch { }
        return json;
    }

    private T? Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(UnwrapEnvelope(json), _jsonOptions);

    // ─── Logging helper ───────────────────────────────────────────────────────

    private void LogOutgoing(string method, string endpoint, int statusCode, long elapsedMs, string? errorBody = null)
    {
        if (statusCode >= 400 || errorBody != null)
        {
            _logger.LogWarning(
                "[WEB→API] {Method} {Endpoint} → {StatusCode} ({ElapsedMs}ms){Error}",
                method, endpoint, statusCode, elapsedMs,
                errorBody != null ? $" | Error: {errorBody}" : string.Empty);
        }
        else
        {
            _logger.LogInformation(
                "[WEB→API] {Method} {Endpoint} → {StatusCode} ({ElapsedMs}ms)",
                method, endpoint, statusCode, elapsedMs);
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            SetAuthHeader();
            var response = await _httpClient.GetAsync(endpoint);
            sw.Stop();

            var content = await response.Content.ReadAsStringAsync();
            LogOutgoing("GET", endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds,
                response.IsSuccessStatusCode ? null : content.Truncate(500));

            if (!response.IsSuccessStatusCode) return default;
            return Deserialize<T>(content);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[WEB→API] GET {Endpoint} failed after {ElapsedMs}ms", endpoint, sw.ElapsedMilliseconds);
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            SetAuthHeader();
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            sw.Stop();

            var responseContent = await response.Content.ReadAsStringAsync();
            LogOutgoing("POST", endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds,
                response.IsSuccessStatusCode ? null : responseContent.Truncate(500));

            if (!response.IsSuccessStatusCode) return default;
            return Deserialize<TResponse>(responseContent);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[WEB→API] POST {Endpoint} failed after {ElapsedMs}ms", endpoint, sw.ElapsedMilliseconds);
            return default;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            SetAuthHeader();
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(endpoint, content);
            sw.Stop();

            var responseContent = await response.Content.ReadAsStringAsync();
            LogOutgoing("PUT", endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds,
                response.IsSuccessStatusCode ? null : responseContent.Truncate(500));

            if (!response.IsSuccessStatusCode) return default;
            return Deserialize<TResponse>(responseContent);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[WEB→API] PUT {Endpoint} failed after {ElapsedMs}ms", endpoint, sw.ElapsedMilliseconds);
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            SetAuthHeader();
            var response = await _httpClient.DeleteAsync(endpoint);
            sw.Stop();
            LogOutgoing("DELETE", endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[WEB→API] DELETE {Endpoint} failed after {ElapsedMs}ms", endpoint, sw.ElapsedMilliseconds);
            return false;
        }
    }

    public async Task<T?> GetWithTokenAsync<T>(string endpoint, string bearerToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            var response = await _httpClient.SendAsync(request);
            sw.Stop();

            var content = await response.Content.ReadAsStringAsync();
            LogOutgoing("GET", endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds,
                response.IsSuccessStatusCode ? null : content.Truncate(500));

            if (!response.IsSuccessStatusCode) return default;
            return Deserialize<T>(content);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[WEB→API] GET (with token) {Endpoint} failed after {ElapsedMs}ms", endpoint, sw.ElapsedMilliseconds);
            return default;
        }
    }

    public async Task<HttpResponseMessage> GetRawAsync(string endpoint)
    {
        SetAuthHeader();
        return await _httpClient.GetAsync(endpoint);
    }

    public async Task<HttpResponseMessage> PostRawAsync<TRequest>(string endpoint, TRequest data)
    {
        SetAuthHeader();
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync(endpoint, content);
    }
}

// ── String helper ─────────────────────────────────────────────────────────────
internal static class WebStringExtensions
{
    internal static string Truncate(this string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "…";
}
