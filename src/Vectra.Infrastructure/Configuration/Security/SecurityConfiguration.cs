using Vectra.Infrastructure.Configuration.Security.AgentAuth;

namespace Vectra.Infrastructure.Configuration.Security;

public class SecurityConfiguration
{
    public AgentAuthConfiguration AgentAuth { get; set; } = new();
}