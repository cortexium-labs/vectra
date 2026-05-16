using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Net;
using Vectra.Application.Abstractions.CircuitBreaker;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Abstractions.RateLimit;
using Vectra.Application.Models;
using Vectra.Domain.Agents;
using Vectra.Domain.Policies;
using Vectra.Middleware;

namespace Vectra.UnitTests.Middleware;

public class ProxyMiddlewareTests
{
    private readonly ILogger<ProxyMiddleware> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProxyMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<ProxyMiddleware>>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
    }

    [Fact]
    public void Constructor_NullNext_Throws()
    {
        var act = () => new ProxyMiddleware(null!, _httpClientFactory);
        act.Should().Throw<ArgumentNullException>().WithParameterName("next");
    }

    [Fact]
    public void Constructor_NullHttpClientFactory_Throws()
    {
        var act = () => new ProxyMiddleware(_ => Task.CompletedTask, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClientFactory");
    }

    [Fact]
    public async Task InvokeAsync_PathNotStartingWithProxy_Returns400()
    {
        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/not-proxy/something");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_InvalidTargetUrl_Returns400()
    {
        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/not-a-url");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_MissingAgentId_Returns401()
    {
        var decisionEngine = Substitute.For<IDecisionEngine>();
        var hitlService = Substitute.For<IHitlService>();
        var agentRepo = Substitute.For<IAgentRepository>();
        var rateLimiter = Substitute.For<IAgentRateLimiter>();
        var circuitBreaker = Substitute.For<ICircuitBreaker>();

        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/http://example.com/api",
            decisionEngine, hitlService, agentRepo, rateLimiter, circuitBreaker);
        context.Response.Body = new MemoryStream();
        // No AgentId in context.Items

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_AgentNotFound_Returns403()
    {
        var agentId = Guid.NewGuid();
        var agentRepo = Substitute.For<IAgentRepository>();
        agentRepo.GetByIdAsync(agentId).Returns((Agent?)null);

        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/http://example.com/api",
            agentRepository: agentRepo);
        context.Items["AgentId"] = agentId;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task InvokeAsync_RevokedAgent_Returns403()
    {
        var agentId = Guid.NewGuid();
        var agent = new Agent("test", "owner", "hash");
        agent.Revoke();

        var agentRepo = Substitute.For<IAgentRepository>();
        agentRepo.GetByIdAsync(agentId).Returns(agent);

        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/http://example.com/api",
            agentRepository: agentRepo);
        context.Items["AgentId"] = agentId;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task InvokeAsync_RateLimitExceeded_Returns429()
    {
        var agentId = Guid.NewGuid();
        var agent = new Agent("test", "owner", "hash");

        var agentRepo = Substitute.For<IAgentRepository>();
        agentRepo.GetByIdAsync(agentId).Returns(agent);

        var rateLimiter = Substitute.For<IAgentRateLimiter>();
        rateLimiter.IsAllowedAsync(agentId, Arg.Any<CancellationToken>()).Returns(false);

        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/http://example.com/api",
            agentRepository: agentRepo, rateLimiter: rateLimiter);
        context.Items["AgentId"] = agentId;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(429);
        context.Response.Headers["Retry-After"].ToString().Should().Be("60");
    }

    [Fact]
    public async Task InvokeAsync_CircuitBreakerOpen_Returns503()
    {
        var agentId = Guid.NewGuid();
        var agent = new Agent("test", "owner", "hash");

        var agentRepo = Substitute.For<IAgentRepository>();
        agentRepo.GetByIdAsync(agentId).Returns(agent);

        var rateLimiter = Substitute.For<IAgentRateLimiter>();
        rateLimiter.IsAllowedAsync(agentId, Arg.Any<CancellationToken>()).Returns(true);

        var circuitBreaker = Substitute.For<ICircuitBreaker>();
        circuitBreaker.IsAllowed("example.com").Returns(false);

        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/http://example.com/api",
            agentRepository: agentRepo, rateLimiter: rateLimiter, circuitBreaker: circuitBreaker);
        context.Items["AgentId"] = agentId;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task InvokeAsync_DecisionDenied_Returns403()
    {
        var agentId = Guid.NewGuid();
        var agent = new Agent("test", "owner", "hash");

        var agentRepo = Substitute.For<IAgentRepository>();
        agentRepo.GetByIdAsync(agentId).Returns(agent);

        var rateLimiter = Substitute.For<IAgentRateLimiter>();
        rateLimiter.IsAllowedAsync(agentId, Arg.Any<CancellationToken>()).Returns(true);

        var circuitBreaker = Substitute.For<ICircuitBreaker>();
        circuitBreaker.IsAllowed(Arg.Any<string>()).Returns(true);

        var decisionEngine = Substitute.For<IDecisionEngine>();
        decisionEngine.EvaluateAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(DecisionResult.Deny("policy violation"));

        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/http://example.com/api",
            decisionEngine: decisionEngine, agentRepository: agentRepo,
            rateLimiter: rateLimiter, circuitBreaker: circuitBreaker);
        context.Items["AgentId"] = agentId;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task InvokeAsync_DecisionHitl_Returns202()
    {
        var agentId = Guid.NewGuid();
        var agent = new Agent("test", "owner", "hash");

        var agentRepo = Substitute.For<IAgentRepository>();
        agentRepo.GetByIdAsync(agentId).Returns(agent);

        var rateLimiter = Substitute.For<IAgentRateLimiter>();
        rateLimiter.IsAllowedAsync(agentId, Arg.Any<CancellationToken>()).Returns(true);

        var circuitBreaker = Substitute.For<ICircuitBreaker>();
        circuitBreaker.IsAllowed(Arg.Any<string>()).Returns(true);

        var decisionEngine = Substitute.For<IDecisionEngine>();
        decisionEngine.EvaluateAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(DecisionResult.Hitl("requires review"));

        var hitlService = Substitute.For<IHitlService>();
        hitlService.SuspendRequestAsync(Arg.Any<RequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("hitl-123");

        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/http://example.com/api",
            decisionEngine: decisionEngine, hitlService: hitlService,
            agentRepository: agentRepo, rateLimiter: rateLimiter, circuitBreaker: circuitBreaker);
        context.Items["AgentId"] = agentId;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(202);
        context.Response.Headers.Location.ToString().Should().Contain("hitl-123");
    }

    [Fact]
    public async Task InvokeAsync_UpstreamHttpError_Records503AndCircuitFailure()
    {
        var agentId = Guid.NewGuid();
        var agent = new Agent("test", "owner", "hash");

        var agentRepo = Substitute.For<IAgentRepository>();
        agentRepo.GetByIdAsync(agentId).Returns(agent);

        var rateLimiter = Substitute.For<IAgentRateLimiter>();
        rateLimiter.IsAllowedAsync(agentId, Arg.Any<CancellationToken>()).Returns(true);

        var circuitBreaker = Substitute.For<ICircuitBreaker>();
        circuitBreaker.IsAllowed(Arg.Any<string>()).Returns(true);

        var decisionEngine = Substitute.For<IDecisionEngine>();
        decisionEngine.EvaluateAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(DecisionResult.Allow());

        // Fake HttpClient that throws
        var handler = new FailingHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/http://example.com/api",
            decisionEngine: decisionEngine, agentRepository: agentRepo,
            rateLimiter: rateLimiter, circuitBreaker: circuitBreaker);
        context.Items["AgentId"] = agentId;
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(503);
        circuitBreaker.Received().RecordFailure("example.com");
    }

    [Fact]
    public async Task InvokeAsync_SuccessfulProxy_CopiesResponseStatusCode()
    {
        var agentId = Guid.NewGuid();
        var agent = new Agent("test", "owner", "hash");

        var agentRepo = Substitute.For<IAgentRepository>();
        agentRepo.GetByIdAsync(agentId).Returns(agent);

        var rateLimiter = Substitute.For<IAgentRateLimiter>();
        rateLimiter.IsAllowedAsync(agentId, Arg.Any<CancellationToken>()).Returns(true);

        var circuitBreaker = Substitute.For<ICircuitBreaker>();
        circuitBreaker.IsAllowed(Arg.Any<string>()).Returns(true);

        var decisionEngine = Substitute.For<IDecisionEngine>();
        decisionEngine.EvaluateAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(DecisionResult.Allow());

        var handler = new FixedResponseHttpMessageHandler(HttpStatusCode.OK, "upstream-body");
        var httpClient = new HttpClient(handler);
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/http://example.com/api",
            decisionEngine: decisionEngine, agentRepository: agentRepo,
            rateLimiter: rateLimiter, circuitBreaker: circuitBreaker);
        context.Items["AgentId"] = agentId;
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
        circuitBreaker.Received().RecordSuccess("example.com");
    }

    [Fact]
    public async Task InvokeAsync_Upstream500_RecordsCircuitFailure()
    {
        var agentId = Guid.NewGuid();
        var agent = new Agent("test", "owner", "hash");

        var agentRepo = Substitute.For<IAgentRepository>();
        agentRepo.GetByIdAsync(agentId).Returns(agent);

        var rateLimiter = Substitute.For<IAgentRateLimiter>();
        rateLimiter.IsAllowedAsync(agentId, Arg.Any<CancellationToken>()).Returns(true);

        var circuitBreaker = Substitute.For<ICircuitBreaker>();
        circuitBreaker.IsAllowed(Arg.Any<string>()).Returns(true);

        var decisionEngine = Substitute.For<IDecisionEngine>();
        decisionEngine.EvaluateAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(DecisionResult.Allow());

        var handler = new FixedResponseHttpMessageHandler(HttpStatusCode.InternalServerError, "error");
        var httpClient = new HttpClient(handler);
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var context = BuildContext("/proxy/http://example.com/api",
            decisionEngine: decisionEngine, agentRepository: agentRepo,
            rateLimiter: rateLimiter, circuitBreaker: circuitBreaker);
        context.Items["AgentId"] = agentId;
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        circuitBreaker.Received().RecordFailure("example.com");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private ProxyMiddleware BuildMiddleware(RequestDelegate next)
        => new(next, _httpClientFactory);

    private static DefaultHttpContext BuildContext(
        string path,
        IDecisionEngine? decisionEngine = null,
        IHitlService? hitlService = null,
        IAgentRepository? agentRepository = null,
        IAgentRateLimiter? rateLimiter = null,
        ICircuitBreaker? circuitBreaker = null)
    {
        decisionEngine ??= Substitute.For<IDecisionEngine>();
        hitlService ??= Substitute.For<IHitlService>();
        agentRepository ??= Substitute.For<IAgentRepository>();
        rateLimiter ??= Substitute.For<IAgentRateLimiter>();
        circuitBreaker ??= Substitute.For<ICircuitBreaker>();

        var services = new ServiceCollection();
        services.AddSingleton(decisionEngine);
        services.AddSingleton(hitlService);
        services.AddSingleton(agentRepository);
        services.AddSingleton(rateLimiter);
        services.AddSingleton(circuitBreaker);
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Path = path;
        context.Request.Method = "GET";
        context.Request.Body = Stream.Null;
        return context;
    }

    // ── Fake handlers ─────────────────────────────────────────────────────

    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("connection refused");
    }

    private sealed class FixedResponseHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;

        public FixedResponseHttpMessageHandler(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body)
            };
            return Task.FromResult(response);
        }
    }
}
