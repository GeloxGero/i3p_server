using System.ComponentModel.DataAnnotations;

namespace i3p_server.Models;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)] // Optional: enforce a minimum length for security
    public string Password { get; set; } = string.Empty;
}