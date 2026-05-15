using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Vectra.Extensions;
using Vectra.Infrastructure.Persistence.Common;

namespace Vectra.UnitTests.Extensions;

public class ApplicationBuilderExtensionsEnsureDbTests
{
    [Fact]
    public async Task EnsureApplicationDatabaseCreated_CallsEachInitializer()
    {
        var initializer1 = Substitute.For<IDatabaseInitializer>();
        var initializer2 = Substitute.For<IDatabaseInitializer>();

        initializer1.EnsureDatabaseCreatedAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        initializer2.EnsureDatabaseCreatedAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var app = BuildApp(initializer1, initializer2);

        await app.EnsureApplicationDatabaseCreated();

        await initializer1.Received(1).EnsureDatabaseCreatedAsync(Arg.Any<CancellationToken>());
        await initializer2.Received(1).EnsureDatabaseCreatedAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureApplicationDatabaseCreated_NoInitializers_DoesNotThrow()
    {
        var app = BuildApp();
        var act = async () => await app.EnsureApplicationDatabaseCreated();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureApplicationDatabaseCreated_ReturnsSameBuilder()
    {
        var app = BuildApp();
        var result = await app.EnsureApplicationDatabaseCreated();
        result.Should().BeSameAs(app);
    }

    private static WebApplication BuildApp(params IDatabaseInitializer[] initializers)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        foreach (var init in initializers)
            builder.Services.AddSingleton(init);
        return builder.Build();
    }
}
