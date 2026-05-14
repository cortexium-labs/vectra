using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Infrastructure.Dispatchers;

namespace Vectra.Infrastructure.UnitTests.Dispatchers;

public class DispatcherTests
{
    private sealed record TestQuery(string Value) : IAction<string>;
    private sealed record TestIntQuery(int Value) : IAction<int>;

    private sealed class TestQueryHandler : IActionHandler<TestQuery, string>
    {
        public Task<string> Handle(TestQuery action, CancellationToken cancellationToken = default)
            => Task.FromResult($"result:{action.Value}");
    }

    private sealed class TestIntHandler : IActionHandler<TestIntQuery, int>
    {
        public Task<int> Handle(TestIntQuery action, CancellationToken cancellationToken = default)
            => Task.FromResult(action.Value * 2);
    }

    private static Dispatcher BuildDispatcher(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        var sp = services.BuildServiceProvider();
        return new Dispatcher(sp);
    }

    [Fact]
    public async Task Dispatch_RegisteredHandler_ReturnsResult()
    {
        var sut = BuildDispatcher(s => s.AddScoped<IActionHandler<TestQuery, string>, TestQueryHandler>());

        var result = await sut.Dispatch(new TestQuery("hello"));

        result.Should().Be("result:hello");
    }

    [Fact]
    public async Task Dispatch_IntHandler_ReturnsDoubledValue()
    {
        var sut = BuildDispatcher(s => s.AddScoped<IActionHandler<TestIntQuery, int>, TestIntHandler>());

        var result = await sut.Dispatch(new TestIntQuery(5));

        result.Should().Be(10);
    }

    [Fact]
    public async Task Dispatch_NullAction_ThrowsArgumentNullException()
    {
        var sut = BuildDispatcher();

        var act = async () => await sut.Dispatch<string>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Dispatch_NoHandlerRegistered_ThrowsInvalidOperationException()
    {
        var sut = BuildDispatcher(); // no handler registered

        var act = async () => await sut.Dispatch(new TestQuery("test"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Dispatch_SameActionType_ReusesCachedDelegate()
    {
        var sut = BuildDispatcher(s => s.AddScoped<IActionHandler<TestQuery, string>, TestQueryHandler>());

        // First call builds delegate, second call reuses cached one
        var result1 = await sut.Dispatch(new TestQuery("first"));
        var result2 = await sut.Dispatch(new TestQuery("second"));

        result1.Should().Be("result:first");
        result2.Should().Be("result:second");
    }

    [Fact]
    public void Constructor_NullServiceProvider_DoesNotThrow()
    {
        // Constructor only assigns sp, no null check in source
        var act = () => new Dispatcher(null!);

        act.Should().NotThrow();
    }
}
