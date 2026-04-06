using System.Security.Claims;
using Vectra.Application.Abstractions.Security;
using Vectra.Domain.Agents;

namespace Vectra.Infrastructure.Security;

public sealed class NoneAgentAuthenticator : IAgentAuthenticator
{
    public AgentAuthScheme Scheme => AgentAuthScheme.None;

    public AgentAuthResult Authenticate(Agent agent)
        => AgentAuthResult.Success();

    public Task<ClaimsPrincipal?> ValidateAsync(string credential, CancellationToken cancellationToken = default)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "anonymous")],
            "None");

        return Task.FromResult<ClaimsPrincipal?>(new ClaimsPrincipal(identity));
    }
}