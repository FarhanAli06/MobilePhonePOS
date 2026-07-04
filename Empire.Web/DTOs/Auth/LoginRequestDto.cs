using System.ComponentModel.DataAnnotations;

namespace Empire.Web.DTOs.Auth;

public class LoginRequestDto
{
    /// <summary>Maps to LoginRequest.Username on the API side.</summary>
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
