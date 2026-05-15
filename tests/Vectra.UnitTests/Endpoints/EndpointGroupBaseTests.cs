using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vectra.Endpoints;

namespace Vectra.UnitTests.Endpoints;

public class EndpointGroupBaseTests
{
    private sealed class ConcreteEndpointGroup : EndpointGroupBase
    {
        public bool MapCalled { get; private set; }
        public override void Map(WebApplication app) => MapCalled = true;
    }

    [Fact]
    public void Map_IsCalled()
    {
        var group = new ConcreteEndpointGroup();
        var app = BuildApp();

        group.Map(app);

        group.MapCalled.Should().BeTrue();
    }

    [Fact]
    public void Map_Agents_DoesNotThrow()
    {
        var app = BuildApp();
        var act = () => new Agents().Map(app);
        act.Should().NotThrow();
    }

    [Fact]
    public void Map_Policies_DoesNotThrow()
    {
        var app = BuildApp();
        var act = () => new Policies().Map(app);
        act.Should().NotThrow();
    }

    [Fact]
    public void Map_Tokens_DoesNotThrow()
    {
        var app = BuildApp();
        var act = () => new Tokens().Map(app);
        act.Should().NotThrow();
    }

    [Fact]
    public void Map_Hitls_DoesNotThrow()
    {
        var app = BuildApp();
        var act = () => new Hitls().Map(app);
        act.Should().NotThrow();
    }

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        return builder.Build();
    }
}
