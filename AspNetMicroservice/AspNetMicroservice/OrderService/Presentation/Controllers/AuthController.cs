using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OrderService.Presentation.Auth;

namespace OrderService.Presentation.Controllers;

/// <summary>
/// Issues JWT tokens. No database — credentials are read from appsettings.json.
/// POST /api/auth/token  →  { "token": "eyJ..." }
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwt;

    public AuthController(JwtSettings jwt) => _jwt = jwt;

    /// <summary>
    /// Exchange username + password for a Bearer token.
    /// </summary>
    [HttpPost("token")]
    public IActionResult Token([FromBody] LoginRequest request)
    {
        // ── Validate credentials (config-based, no DB) ──────────────────────
        if (!request.Username.Equals(_jwt.AdminUsername, StringComparison.Ordinal) ||
            !request.Password.Equals(_jwt.AdminPassword, StringComparison.Ordinal))
        {
            return Unauthorized(new { error = "Invalid username or password." });
        }

        // ── Build claims ─────────────────────────────────────────────────────
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   request.Username),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, "Admin")
        };

        // ── Sign token ───────────────────────────────────────────────────────
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires     = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expires,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token     = tokenString,
            expiresAt = expires,
            type      = "Bearer"
        });
    }
}

/// <summary>Login request body.</summary>
public sealed record LoginRequest(string Username, string Password);
