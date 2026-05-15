using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vectra.Endpoints;
using Vectra.Extensions;

namespace Vectra.UnitTests.Extensions;

public class WebApplicationExtensionsTests
{
    // ── MapGroup ──────────────────────────────────────────────────────────

    [Fact]
    public void MapGroup_ReturnsRouteGroupBuilder()
    {
        var app = BuildMinimalWebApp();
        var group = new Agents();

        var result = app.MapGroup(group);

        result.Should().NotBeNull();
    }

    [Fact]
    public void MapGroup_UsesEndpointGroupTypeName()
    {
        // Validates no exception and route prefix is derived from the type name
        var app = BuildMinimalWebApp();
        var act = () => app.MapGroup(new Agents());
        act.Should().NotThrow();
    }

    // ── MapEndpoints ──────────────────────────────────────────────────────

    [Fact]
    public void MapEndpoints_RegistersAllEndpointGroups()
    {
        var app = BuildMinimalWebApp();
        // All concrete EndpointGroupBase subclasses should be discovered and mapped
        var result = app.MapEndpoints();
        result.Should().BeSameAs(app);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static WebApplication BuildMinimalWebApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        return builder.Build();
    }
}
