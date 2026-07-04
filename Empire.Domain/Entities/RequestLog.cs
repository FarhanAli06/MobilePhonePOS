using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Empire.Domain.Entities;

/// <summary>
/// Persisted record of every HTTP request processed by the API
/// and every outgoing HTTP call made by the Web layer.
/// </summary>
[Table("RequestLogs")]
public class RequestLog
{
    [Key]
    public long Id { get; set; }

    // ── Source ────────────────────────────────────────────────────────────────

    /// <summary>"API" = incoming request to the API; "WEB" = outgoing call from the Web layer.</summary>
    [MaxLength(10)]
    public string Source { get; set; } = "API";

    // ── Request ───────────────────────────────────────────────────────────────

    [MaxLength(10)]
    public string HttpMethod { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Path { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? QueryString { get; set; }

    /// <summary>Request body (truncated to 4 KB).</summary>
    [MaxLength(4096)]
    public string? RequestBody { get; set; }

    [MaxLength(2000)]
    public string? RequestHeaders { get; set; }

    // ── Identity ──────────────────────────────────────────────────────────────

    [MaxLength(100)]
    public string? UserId { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    public int? ShopId { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    // ── Response ──────────────────────────────────────────────────────────────

    public int StatusCode { get; set; }

    /// <summary>Response body — only captured on non-2xx responses (truncated to 4 KB).</summary>
    [MaxLength(4096)]
    public string? ResponseBody { get; set; }

    // ── Timing ────────────────────────────────────────────────────────────────

    public long ElapsedMilliseconds { get; set; }

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    // ── Error ─────────────────────────────────────────────────────────────────

    [MaxLength(4096)]
    public string? ErrorMessage { get; set; }

    public bool IsError => StatusCode >= 400;
}
