using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using i3p_server.Models;

namespace i3p_server.Services;

public class AuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Generates a signed JWT containing the user's Id (as NameIdentifier),
    /// Name, and Email. The token is valid for 7 days.
    /// </summary>
    public string GenerateToken(Users user)
    {
        var jwtKey = _config["Jwt:Key"]
                     ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // ── Claims ────────────────────────────────────────────────────────────
        // NameIdentifier is the claim GetProfile reads with:
        //   User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name,           user.Name),
            new Claim(ClaimTypes.Email,          user.Email),
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"]   ?? "i3p-server",
            audience:           _config["Jwt:Audience"] ?? "i3p-client",
            claims:             claims,
            expires:            DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}