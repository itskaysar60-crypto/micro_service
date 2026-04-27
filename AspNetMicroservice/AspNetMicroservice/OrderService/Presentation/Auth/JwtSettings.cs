namespace OrderService.Presentation.Auth;

/// <summary>
/// Strongly-typed binding for the "Jwt" section in appsettings.json.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>Secret key used to sign tokens (min 32 chars for HS256).</summary>
    public string SecretKey    { get; set; } = string.Empty;
    /// <summary>Token issuer — must match the value checked by middleware.</summary>
    public string Issuer       { get; set; } = "OrderService";
    /// <summary>Token audience — must match the value checked by middleware.</summary>
    public string Audience     { get; set; } = "OrderServiceClients";
    /// <summary>How long (minutes) the token is valid.</summary>
    public int    ExpiryMinutes { get; set; } = 60;

    // ── Hard-coded credentials (no DB) ──────────────────────────────────────
    // In production replace with a list from config / env vars.
    public string AdminUsername { get; set; } = "admin";
    public string AdminPassword { get; set; } = "Admin@1234";
}
