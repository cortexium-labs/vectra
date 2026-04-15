namespace Vectra.BuildingBlocks.Configuration.Security.AgentAuth;

public class SelfSignedProvider
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(15);
}