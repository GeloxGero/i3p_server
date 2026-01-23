using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using i3p_server.Models;
using Microsoft.IdentityModel.Tokens;

namespace i3p_server.Services;

public class AuthService
{
    private readonly IConfiguration _configuration;

    // The IDE can now find _configuration because it's injected here
    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Changed to 'public' so your Controller can use it
    public string GenerateToken(Users user)
    {
        // Add a check to ensure the Key isn't null
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing in appsettings.json");
        
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Authority.ToString()),
            new Claim("Name", user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8), // Use UtcNow for consistency
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}