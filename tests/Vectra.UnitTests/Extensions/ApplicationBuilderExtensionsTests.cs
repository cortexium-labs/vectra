using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Vectra.Extensions;

namespace Vectra.UnitTests.Extensions;

public class ApplicationBuilderExtensionsTests
{
    // ── UseVectraCustomException ───────────────────────────────────────────

    [Fact]
    public void UseVectraCustomException_ReturnsSameBuilder()
    {
        var builder = Substitute.For<IApplicationBuilder>();
        builder.Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>()).Returns(builder);
        builder.New().Returns(builder);

        var result = builder.UseVectraCustomException();

        result.Should().BeSameAs(builder);
    }

    // ── UseVectraCustomHeaders ─────────────────────────────────────────────

    [Fact]
    public void UseVectraCustomHeaders_ReturnsSameBuilder()
    {
        var builder = Substitute.For<IApplicationBuilder>();
        builder.Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>()).Returns(builder);
        builder.New().Returns(builder);

        var result = builder.UseVectraCustomHeaders();

        result.Should().BeSameAs(builder);
    }
}
