using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Abstractions.Security;
using Vectra.Application.Models;
using Vectra.Domain.Agents;
using Yarp.ReverseProxy.Forwarder;

namespace Vectra.Middleware;

public class ProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpForwarder _forwarder;
    private readonly ILogger<ProxyMiddleware> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProxyMiddleware(
        RequestDelegate next,
        IHttpForwarder forwarder,
        ILogger<ProxyMiddleware> logger,
        IHttpClientFactory httpClientFactory)
    {
        _next = next;
        _forwarder = forwarder;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Extract raw target URL from the path
        var fullPath = context.Request.Path.ToString();
        if (!fullPath.StartsWith("/proxy/"))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid proxy path. Expected /proxy/<full-url>");
            return;
        }

        var targetUrlString = fullPath.Substring("/proxy/".Length);
        if (!Uri.TryCreate(targetUrlString, UriKind.Absolute, out var targetUri))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid target URL in proxy path");
            return;
        }

        // 2. Store the destination base URI
        var destinationBaseUri = $"{targetUri.Scheme}://{targetUri.Authority}";

        // 3. Override request path and query for YARP
        context.Request.Path = targetUri.AbsolutePath;
        context.Request.QueryString = new QueryString(targetUri.Query);

        // 4. Resolve services
        var decisionEngine = context.RequestServices.GetRequiredService<IDecisionEngine>();
        var hitlService = context.RequestServices.GetRequiredService<IHitlService>();
        var agentRepository = context.RequestServices.GetRequiredService<IAgentRepository>();

        // 5. Authentication – scheme-aware
        var authScheme = context.Items.TryGetValue("AuthScheme", out var schemeObj) && schemeObj is AgentAuthScheme scheme
            ? scheme
            : AgentAuthScheme.Jwt; // default to JWT for backward compat

        Guid agentId;
        double trustScore;

        if (authScheme == AgentAuthScheme.None)
        {
            // No authentication – use defaults set by AgentAuthMiddleware
            agentId = context.Items.TryGetValue("AgentId", out var id) && id is Guid g ? g : Guid.Empty;
            trustScore = context.Items.TryGetValue("TrustScore", out var ts) && ts is double t ? t : 0.5;
        }
        else
        {
            // JWT (or future Basic) – require valid agent identity
            if (!context.Items.TryGetValue("AgentId", out var agentIdObj) || agentIdObj is not Guid authenticatedId)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing or invalid authentication");
                return;
            }

            agentId = authenticatedId;

            var agent = await agentRepository.GetByIdAsync(agentId);
            if (agent == null || agent.Status != AgentStatus.Active)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Agent is not active");
                return;
            }

            trustScore = agent.TrustScore;
        }

        // 6. Build RequestContext for policy evaluation (read body)
        context.Request.EnableBuffering();
        var requestContext = new RequestContext
        {
            Method = context.Request.Method,
            Path = targetUri.PathAndQuery,
            Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            AgentId = agentId,
            TrustScore = trustScore,
            Body = await ReadBodyAsync(context.Request)
        };

        var decision = await decisionEngine.EvaluateAsync(requestContext);

        if (decision.IsDenied)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync(decision.Reason ?? "Access denied");
            return;
        }

        if (decision.IsHitl)
        {
            var hitlId = await hitlService.SuspendRequestAsync(requestContext, decision.Reason ?? "HITL required");
            context.Response.StatusCode = 202;
            context.Response.Headers.Location = $"/hitl/status/{hitlId}";
            await context.Response.WriteAsync($"Request pending approval. Poll {context.Response.Headers.Location}");
            return;
        }

        // 7. Forward the request with CORRECT headers
        context.Request.Body.Position = 0;

        // Create a new HttpRequestMessage for manual forwarding (or use YARP with transforms)
        var httpClient = _httpClientFactory.CreateClient();
        var proxyRequest = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = targetUri,
            Content = context.Request.Body.Length > 0 ? new StreamContent(context.Request.Body) : null
        };

        // Copy headers, but exclude Aegis-specific ones
        foreach (var header in context.Request.Headers)
        {
            // Skip headers that must NOT be forwarded
            if (header.Key == "Authorization" ||          // JWT for Aegis, not for upstream
                header.Key == "Host" ||                   // Will be set from RequestUri
                header.Key == "Connection" ||
                header.Key == "Content-Length")           // Handled by HttpClient
                continue;

            // Keep all other headers (including Accept, AgentId, etc.)
            proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
        }

        // Optionally inject a real API key (if needed for the target)
        // proxyRequest.Headers.Add("X-API-Key", "your-secret-key");

        // Send the request
        var response = await httpClient.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead);

        // Copy response back
        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
            context.Response.Headers[header.Key] = header.Value.ToString();
        foreach (var header in response.Content.Headers)
            context.Response.Headers[header.Key] = header.Value.ToString();

        await response.Content.CopyToAsync(context.Response.Body);
    }

    private async Task<string?> ReadBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == null || request.ContentLength == 0)
            return null;

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }
}