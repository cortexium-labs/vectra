namespace Vectra.Infrastructure.Configuration.Security.AgentAuth;

public class AgentAuthConfiguration
{
    public JwtProviderType Provider { get; set; } = JwtProviderType.SelfSigned;

    // ── Self-signed settings ──────────────────────────────────────────
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 15;

    // ── External provider settings (Keycloak, Auth0, Azure AD, Custom) ─
    public string Authority { get; set; } = string.Empty;
    public string MetadataUrl { get; set; } = string.Empty;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
}