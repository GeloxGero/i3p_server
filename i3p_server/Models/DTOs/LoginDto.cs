using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace i3p_server.Models;

public class LoginDto
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)] // Optional: enforce a minimum length for security
    [JsonPropertyName("password")]
    public string password { get; set; } = string.Empty;
}