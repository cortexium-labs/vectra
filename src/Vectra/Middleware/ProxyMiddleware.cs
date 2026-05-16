using System.Text.Json;
using Vectra.Application.Abstractions.CircuitBreaker;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Abstractions.RateLimit;
using Vectra.Application.Models;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Decision;

namespace Vectra.Middleware;

public class ProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProxyMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
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

        Guid agentId;
        double trustScore;

        // 5. JWT – require valid agent identity
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

        // 6. Rate limiting – 429 if agent exceeded requests/min
        var rateLimiter = context.RequestServices.GetRequiredService<IAgentRateLimiter>();
        if (!await rateLimiter.IsAllowedAsync(agentId, context.RequestAborted))
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsync("Rate limit exceeded. Try again in 60 seconds.");
            return;
        }

        // 7. Circuit breaker – 503 if upstream is currently open
        var circuitBreaker = context.RequestServices.GetRequiredService<ICircuitBreaker>();
        var upstreamHost = targetUri.Host;
        if (!circuitBreaker.IsAllowed(upstreamHost))
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync($"Upstream '{upstreamHost}' is temporarily unavailable.");
            return;
        }
        context.Request.EnableBuffering();
        var requestContext = new RequestContext
        {
            Method = context.Request.Method,
            Path = targetUri.PathAndQuery,
            TargetUrl = targetUri.ToString(),
            Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            AgentId = agentId,
            PolicyName = agent.PolicyName,
            TrustScore = trustScore,
            Body = await ReadBodyAsync(context.Request)
        };

        var decision = await decisionEngine.EvaluateAsync(requestContext, context.RequestAborted);

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

        // 8. Forward the request with CORRECT headers
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
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead);
            circuitBreaker.RecordSuccess(upstreamHost);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            circuitBreaker.RecordFailure(upstreamHost);
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync($"Upstream '{upstreamHost}' is unavailable.");
            return;
        }

        // Copy response back
        context.Response.StatusCode = (int)response.StatusCode;
        if ((int)response.StatusCode >= 500)
            circuitBreaker.RecordFailure(upstreamHost);

        // Headers that must not be copied — ASP.NET Core / HttpClient manage these
        static bool IsRestrictedHeader(string name) =>
            name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Content-Length",    StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Connection",        StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Keep-Alive",        StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Upgrade",           StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Trailer",           StringComparison.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            if (!IsRestrictedHeader(header.Key))
                context.Response.Headers[header.Key] = string.Join(", ", header.Value);
        }
        foreach (var header in response.Content.Headers)
        {
            if (!IsRestrictedHeader(header.Key))
                context.Response.Headers[header.Key] = string.Join(", ", header.Value);
        }

        await response.Content.CopyToAsync(context.Response.Body);
    }

    private async Task<string?> ReadBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0) return null;

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        var isJson = request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true
                  || request.ContentType?.Contains("+json", StringComparison.OrdinalIgnoreCase) == true;

        if (!isJson) return body; // keep raw text for semantic/risk analysis

        try
        {
            return JsonToIntentText.Convert(body);
        }
        catch (JsonException)
        {
            return body; // resilient fallback
        }
    }
}