namespace Vectra.BuildingBlocks.Configuration.Security.AgentAuth;

public class AgentAuthConfiguration
{
    public AgentAuthProviderType Provider { get; set; } = AgentAuthProviderType.SelfSigned;
    public SelfSignedProvider SelfSigned { get; set; } = new();
    public JwtProvider Jwt { get; set; } = new();
}