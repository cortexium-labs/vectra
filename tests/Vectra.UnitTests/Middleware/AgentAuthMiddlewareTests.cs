using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vectra.Application.Abstractions.Security;

namespace Vectra.UnitTests.Middleware;

public class AgentAuthMiddlewareTests
{
    private readonly ILogger<Vectra.Middleware.AgentAuthMiddleware> _logger;
    private readonly IAgentAuthenticator _authenticator;

    public AgentAuthMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<Vectra.Middleware.AgentAuthMiddleware>>();
        _authenticator = Substitute.For<IAgentAuthenticator>();
    }

    private HttpContext BuildContext(string? authHeader = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_authenticator);
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };

        if (authHeader is not null)
            context.Request.Headers.Authorization = authHeader;

        return context;
    }

    [Fact]
    public async Task InvokeAsync_NoAuthHeader_CallsNextWithoutSettingAgentId()
    {
        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        var middleware = new Vectra.Middleware.AgentAuthMiddleware(next, _logger);

        var context = BuildContext();
        await middleware.InvokeAsync(context);

        context.Items.ContainsKey("AgentId").Should().BeFalse();
        await next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_EmptyAuthHeader_CallsNextWithoutSettingAgentId()
    {
        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        var middleware = new Vectra.Middleware.AgentAuthMiddleware(next, _logger);

        var context = BuildContext("   ");
        await middleware.InvokeAsync(context);

        context.Items.ContainsKey("AgentId").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_InvalidAuthScheme_DoesNotSetAgentId()
    {
        _authenticator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ClaimsPrincipal?)null);

        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        var middleware = new Vectra.Middleware.AgentAuthMiddleware(next, _logger);

        var context = BuildContext("Basic sometoken");
        await middleware.InvokeAsync(context);

        context.Items.ContainsKey("AgentId").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ValidBearerToken_SetsAgentId()
    {
        var agentId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, agentId.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _authenticator.ValidateAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(principal);

        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        var middleware = new Vectra.Middleware.AgentAuthMiddleware(next, _logger);

        var context = BuildContext("Bearer valid-token");
        await middleware.InvokeAsync(context);

        context.Items["AgentId"].Should().Be(agentId);
    }

    [Fact]
    public async Task InvokeAsync_ValidBearerToken_WithTrustScore_SetsTrustScore()
    {
        var agentId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, agentId.ToString()),
            new Claim("trust_score", "0.85")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _authenticator.ValidateAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(principal);

        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        var middleware = new Vectra.Middleware.AgentAuthMiddleware(next, _logger);

        var context = BuildContext("Bearer valid-token");
        await middleware.InvokeAsync(context);

        context.Items["TrustScore"].Should().Be(0.85);
    }

    [Fact]
    public async Task InvokeAsync_ValidatorReturnsNull_DoesNotSetAgentId()
    {
        _authenticator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ClaimsPrincipal?)null);

        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        var middleware = new Vectra.Middleware.AgentAuthMiddleware(next, _logger);

        var context = BuildContext("Bearer invalid-token");
        await middleware.InvokeAsync(context);

        context.Items.ContainsKey("AgentId").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_BearerWithNonGuidSub_DoesNotSetAgentId()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _authenticator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(principal);

        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        var middleware = new Vectra.Middleware.AgentAuthMiddleware(next, _logger);

        var context = BuildContext("Bearer some-token");
        await middleware.InvokeAsync(context);

        context.Items.ContainsKey("AgentId").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_BearerWithSubClaim_SetsAgentId()
    {
        var agentId = Guid.NewGuid();
        var claims = new[] { new Claim("sub", agentId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _authenticator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(principal);

        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        var middleware = new Vectra.Middleware.AgentAuthMiddleware(next, _logger);

        var context = BuildContext("Bearer some-token");
        await middleware.InvokeAsync(context);

        context.Items["AgentId"].Should().Be(agentId);
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext()
    {
        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        var middleware = new Vectra.Middleware.AgentAuthMiddleware(next, _logger);

        var context = BuildContext();
        await middleware.InvokeAsync(context);

        await next.Received(1).Invoke(context);
    }
}
