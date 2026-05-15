using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vectra.Extensions;

namespace Vectra.UnitTests.Extensions;

public class EndpointRouteBuilderExtensionsTests
{
    // The extension methods delegate to the underlying IEndpointRouteBuilder.
    // We use a real WebApplication to test them end-to-end without mocking internal ASP.NET Core types.

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        return builder.Build();
    }

    [Fact]
    public void MapGet_ReturnsBuilder_AndDoesNotThrow()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok("get");

        IEndpointRouteBuilder result = null!;
        var act = () => result = app.MapGet(handler, "/test-get");

        act.Should().NotThrow();
        result.Should().NotBeNull();
    }

    [Fact]
    public void MapPost_ReturnsBuilder_AndDoesNotThrow()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok("post");

        IEndpointRouteBuilder result = null!;
        var act = () => result = app.MapPost(handler, "/test-post");

        act.Should().NotThrow();
        result.Should().NotBeNull();
    }

    [Fact]
    public void MapPut_ReturnsBuilder_AndDoesNotThrow()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok("put");

        IEndpointRouteBuilder result = null!;
        var act = () => result = app.MapPut(handler, "/test-put");

        act.Should().NotThrow();
        result.Should().NotBeNull();
    }

    [Fact]
    public void MapDelete_ReturnsBuilder_AndDoesNotThrow()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok("delete");

        IEndpointRouteBuilder result = null!;
        var act = () => result = app.MapDelete(handler, "/test-delete");

        act.Should().NotThrow();
        result.Should().NotBeNull();
    }

    [Fact]
    public void MapGet_DefaultPattern_DoesNotThrow()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok();

        var act = () => app.MapGet(handler);

        act.Should().NotThrow();
    }

    [Fact]
    public void MapPost_DefaultPattern_DoesNotThrow()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok();

        var act = () => app.MapPost(handler);

        act.Should().NotThrow();
    }

    [Fact]
    public void MapPut_DefaultPattern_DoesNotThrow()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok();

        var act = () => app.MapPut(handler);

        act.Should().NotThrow();
    }

    [Fact]
    public void MapDelete_DefaultPattern_DoesNotThrow()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok();

        var act = () => app.MapDelete(handler);

        act.Should().NotThrow();
    }

    [Fact]
    public void MapGet_ReturnsSameBuilderInstance()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok();

        var result = app.MapGet(handler, "/g");

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void MapPost_ReturnsSameBuilderInstance()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok();

        var result = app.MapPost(handler, "/p");

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void MapPut_ReturnsSameBuilderInstance()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok();

        var result = app.MapPut(handler, "/u");

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void MapDelete_ReturnsSameBuilderInstance()
    {
        var app = BuildApp();
        Delegate handler = static () => Results.Ok();

        var result = app.MapDelete(handler, "/d");

        result.Should().BeSameAs(app);
    }
}
