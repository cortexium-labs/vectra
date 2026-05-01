using System.Security.Claims;
using Vectra.Application.Abstractions.Security;

namespace Vectra.Middleware;

public class AgentAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AgentAuthMiddleware> _logger;

    public AgentAuthMiddleware(RequestDelegate next, ILogger<AgentAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authenticator = context.RequestServices.GetRequiredService<IAgentAuthenticator>();
        await AttachFromHeaderAsync(context, authenticator, "Bearer");
        await _next(context);
    }

    private async Task AttachFromHeaderAsync(
        HttpContext context,
        IAgentAuthenticator authenticator,
        string expectedPrefix)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
            return;

        var parts = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !parts[0].Equals(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            return;

        var credential = parts[1];
        var principal = await authenticator.ValidateAsync(credential, CancellationToken.None);
        if (principal is null)
            return;

        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? principal.FindFirstValue("sub");

        if (Guid.TryParse(sub, out var agentId))
        {
            context.Items["AgentId"] = agentId;

            var trustClaim = principal.FindFirstValue("trust_score");
            if (double.TryParse(trustClaim, out var trust))
                context.Items["TrustScore"] = trust;
        }
    }
}