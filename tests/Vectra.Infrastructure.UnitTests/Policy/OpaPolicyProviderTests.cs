using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;
using System.Text.Json;
using Vectra.Application.Abstractions.Executions;
using Vectra.BuildingBlocks.Configuration.Policy;
using Vectra.Infrastructure.Policy.Providers;

namespace Vectra.Infrastructure.UnitTests.Policy;

public class OpaPolicyProviderTests
{
    private static OpaPolicyProvider CreateSut(IHttpClientFactory factory, OpaPolicyConfiguration? opa = null)
    {
        var config = new PolicyConfiguration
        {
            Enabled = true,
            Providers = new PolicyProviders
            {
                Opa = opa ?? new OpaPolicyConfiguration
                {
                    BaseUrl = "http://opa.local",
                    Path = "/v1/data/vectra/allow",
                    Timeout = TimeSpan.FromSeconds(5)
                }
            }
        };
        return new OpaPolicyProvider(
            factory,
            Options.Create(config),
            NullLogger<OpaPolicyProvider>.Instance);
    }

    [Fact]
    public async Task EvaluateAsync_NullOpaConfig_ReturnsDeny()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        var config = new PolicyConfiguration
        {
            Providers = new PolicyProviders { Opa = null! }
        };
        var sut = new OpaPolicyProvider(factory, Options.Create(config), NullLogger<OpaPolicyProvider>.Instance);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsDenied.Should().BeTrue();
        result.Reason.Should().Contain("OPA");
    }

    [Fact]
    public async Task EvaluateAsync_EmptyBaseUrl_ReturnsDeny()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        var sut = CreateSut(factory, new OpaPolicyConfiguration { BaseUrl = string.Empty });

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsDenied.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsFalse_ReturnsDeny()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"result": false}""");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsDenied.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsTrue_ReturnsAllow()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"result": true}""");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsEffectAllow_ReturnsAllow()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"result": {"effect": "allow", "reason": "ok"}}""");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Be("ok");
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsEffectHitl_ReturnsHitl()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"result": {"effect": "hitl", "reason": "needs review"}}""");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsHitl.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsEffectDeny_ReturnsDeny()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"result": {"effect": "deny", "reason": "blocked"}}""");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsDenied.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsAllowTrue_ReturnsAllow()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"result": {"allow": true}}""");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsHitlTrue_ReturnsHitl()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"result": {"hitl": true}}""");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsHitl.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsObjectWithNoKnownProperties_ReturnsDeny()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"result": {"unknown": "value"}}""");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsDenied.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsMissingResult_ReturnsDeny()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"data": {}}""");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsDenied.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsUnsuccessfulStatus_ReturnsDeny()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "error");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsDenied.Should().BeTrue();
        result.Reason.Should().Contain("500");
    }

    [Fact]
    public async Task EvaluateAsync_OpaReturnsUnsupportedFormat_ReturnsDeny()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"result": 42}""");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));

        var sut = CreateSut(factory);

        var result = await sut.EvaluateAsync("policy", new Dictionary<string, object>(), CancellationToken.None);

        result.IsDenied.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullHttpClientFactory_Throws()
    {
        var act = () => new OpaPolicyProvider(
            null!, Options.Create(new PolicyConfiguration()), NullLogger<OpaPolicyProvider>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
    }
}
