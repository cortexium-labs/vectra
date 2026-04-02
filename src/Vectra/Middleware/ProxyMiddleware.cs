using Vectra.Core.DTOs;
using Vectra.Core.Entities;
using Vectra.Core.Interfaces;
using Vectra.Core.UseCases;
using Yarp.ReverseProxy.Forwarder;

namespace Vectra.Middleware;

public class ProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpForwarder _forwarder;
    private readonly ILogger<ProxyMiddleware> _logger;
    private readonly EvaluateRequestUseCase _evaluateUseCase;
    private readonly IHitlService _hitlService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAgentRepository _agentRepository;

    public ProxyMiddleware(
        RequestDelegate next,
        IHttpForwarder forwarder,
        ILogger<ProxyMiddleware> logger,
        EvaluateRequestUseCase evaluateUseCase,
        IHitlService hitlService,
        IHttpClientFactory httpClientFactory,
        IAgentRepository agentRepository)
    {
        _next = next;
        _forwarder = forwarder;
        _logger = logger;
        _evaluateUseCase = evaluateUseCase;
        _hitlService = hitlService;
        _httpClientFactory = httpClientFactory;
        _agentRepository = agentRepository;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Get agent ID from JWT (attached by JwtMiddleware)
        if (!context.Items.TryGetValue("AgentId", out var agentIdObj) || agentIdObj is not Guid agentId)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing or invalid authentication");
            return;
        }

        // 2. Fetch agent data (including trust score)
        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent == null || agent.Status != AgentStatus.Active)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Agent is not active");
            return;
        }

        // 3. Build RequestContext (without reading body yet – we'll read if needed)
        var requestContext = new RequestContext
        {
            Method = context.Request.Method,
            Path = context.Request.Path + context.Request.QueryString,
            Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            AgentId = agentId,
            TrustScore = agent.TrustScore,
        };

        // 4. Evaluate policy (may need body)
        // To avoid reading body unnecessarily, we only read it if the decision engine requires it.
        // For simplicity, we'll read it now and store.
        // But we need to ensure we can read the body multiple times (for forwarding).
        // So enable buffering.
        context.Request.EnableBuffering();
        requestContext.Body = await ReadBodyAsync(context.Request);

        var decision = await _evaluateUseCase.ExecuteAsync(requestContext);

        if (decision.IsDenied)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(decision.Reason ?? "Access denied");
            return;
        }

        if (decision.IsHitl)
        {
            var hitlId = await _hitlService.SuspendRequestAsync(requestContext, decision.Reason ?? "HITL required");
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            context.Response.Headers.Location = $"/hitl/status/{hitlId}";
            await context.Response.WriteAsync($"Request pending approval. Poll {context.Response.Headers.Location}");
            return;
        }

        // 5. Allowed – forward using YARP
        // Determine destination URI from the original request's host header.
        // The host header should be the target API's host (e.g., api.github.com).
        var targetHost = context.Request.Host.Host;
        var targetScheme = context.Request.Scheme; // use same scheme as incoming request
        var destinationUri = $"{targetScheme}://{targetHost}";

        // If the agent uses a custom header to specify the target, you could use that instead.
        // For example: var destination = context.Request.Headers["X-Target-Url"].FirstOrDefault();

        // Create an HttpClient for forwarding (use a shared client, but careful with DNS).
        // We can get a named client from the factory.
        var httpClient = _httpClientFactory.CreateClient("ProxyForwarder");

        // Reset the request body position to beginning for forwarding
        context.Request.Body.Position = 0;

        // Forward the request
        await _forwarder.SendAsync(context, destinationUri, httpClient);
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