using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MusicDistribution.Api.Controllers;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Minimal authentication endpoint for the take-home exercise.
/// There is no user/registration flow: a single admin account is configured via
/// the AdminUser:Username/AdminUser:Password config keys - set via `dotnet user-secrets`
/// locally or environment variables elsewhere, never committed in appsettings.json (see
/// README.md "Configuring secrets") - and exchanged for a JWT here. In a real system
/// this would be backed by ASP.NET Identity (or an external IdP) with hashed
/// passwords, not a config-file comparison.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var configuredUser = _config["AdminUser:Username"];
        var configuredPassword = _config["AdminUser:Password"];

        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        // Constant-time-ish comparison isn't critical here since this is a single
        // hardcoded demo account, but real credential checks should never use a
        // naive == on plaintext passwords - see DECISIONS.md.
        if (request.Username != configuredUser || request.Password != configuredPassword)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var jwtKey = _config["Jwt:Key"]!;
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt = token.ValidTo
        });
    }
}
