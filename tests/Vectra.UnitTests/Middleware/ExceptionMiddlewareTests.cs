using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Vectra.UnitTests.Middleware;

public class ExceptionMiddlewareTests
{
    private readonly ILogger<Vectra.Middleware.ExceptionMiddleware> _logger;

    public ExceptionMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<Vectra.Middleware.ExceptionMiddleware>>();
    }

    [Fact]
    public void Constructor_NullNext_ThrowsArgumentNullException()
    {
        var act = () => new Vectra.Middleware.ExceptionMiddleware(null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("next");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new Vectra.Middleware.ExceptionMiddleware(_ => Task.CompletedTask, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task Invoke_NextSucceeds_DoesNotWriteErrorResponse()
    {
        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        var middleware = new Vectra.Middleware.ExceptionMiddleware(next, _logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Invoke_NextThrows_Returns500WithProblemDetails()
    {
        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Throws(new InvalidOperationException("boom"));
        var middleware = new Vectra.Middleware.ExceptionMiddleware(next, _logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Invoke_NextThrows_LogsError()
    {
        var exception = new InvalidOperationException("test error");
        var next = Substitute.For<RequestDelegate>();
        next(Arg.Any<HttpContext>()).Throws(exception);
        var middleware = new Vectra.Middleware.ExceptionMiddleware(next, _logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
