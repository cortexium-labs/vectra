using Microsoft.AspNetCore.Http;
using NSubstitute;
using Vectra.Application.Abstractions.Versioning;

namespace Vectra.UnitTests.Middleware;

public class VersionHeaderMiddlewareTests
{
    [Fact]
    public void Constructor_NullNext_ThrowsArgumentNullException()
    {
        var version = Substitute.For<IVersion>();
        var act = () => new Vectra.Middleware.VersionHeaderMiddleware(null!, version);
        act.Should().Throw<ArgumentNullException>().WithParameterName("next");
    }

    [Fact]
    public void Constructor_NullVersion_ThrowsArgumentNullException()
    {
        var act = () => new Vectra.Middleware.VersionHeaderMiddleware(_ => Task.CompletedTask, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("version");
    }

    [Fact]
    public async Task Invoke_AddsVersionHeader()
    {
        var expectedVersion = new Version(1, 2, 3, 4);
        var version = Substitute.For<IVersion>();
        version.Version.Returns(expectedVersion);

        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);

        var middleware = new Vectra.Middleware.VersionHeaderMiddleware(next, version);
        var context = new DefaultHttpContext();

        await middleware.Invoke(context);

        context.Response.Headers["Vectra-Version"].ToString().Should().Be(expectedVersion.ToString());
    }

    [Fact]
    public async Task Invoke_CallsNext()
    {
        var version = Substitute.For<IVersion>();
        version.Version.Returns(new Version(1, 0, 0, 0));

        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);

        var middleware = new Vectra.Middleware.VersionHeaderMiddleware(next, version);
        var context = new DefaultHttpContext();

        await middleware.Invoke(context);

        await next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_VersionHeaderReflectsCurrentVersion()
    {
        var version = Substitute.For<IVersion>();
        version.Version.Returns(new Version(2, 5, 0, 0));

        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);

        var middleware = new Vectra.Middleware.VersionHeaderMiddleware(next, version);
        var context = new DefaultHttpContext();

        await middleware.Invoke(context);

        context.Response.Headers["Vectra-Version"].ToString().Should().Be("2.5.0.0");
    }
}
