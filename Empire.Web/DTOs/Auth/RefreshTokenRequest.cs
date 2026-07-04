using System.ComponentModel.DataAnnotations;

namespace Empire.Web.DTOs.Auth;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
