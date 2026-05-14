using FluentAssertions;
using Vectra.Infrastructure.Serializations.Json;
using Vectra.Infrastructure.Exceptions;

namespace Vectra.Infrastructure.UnitTests.Serializations;

public class JsonSerializerTests
{
    private readonly JsonSerializer _sut = new();

    [Fact]
    public void Serialize_SimpleObject_ReturnsJsonString()
    {
        var obj = new { Name = "test", Value = 42 };

        var json = _sut.Serialize(obj);

        json.Should().Contain("\"name\"");
        json.Should().Contain("\"test\"");
        json.Should().Contain("42");
    }

    [Fact]
    public void Serialize_IsNotIndented()
    {
        var obj = new { Name = "test" };

        var json = _sut.Serialize(obj);

        json.Should().NotContain("\n");
    }

    [Fact]
    public void SerializePretty_IsIndented()
    {
        var obj = new { Name = "test", Value = 42 };

        var json = _sut.SerializePretty(obj);

        json.Should().Contain("\n");
    }

    [Fact]
    public void Serialize_NullObject_ReturnsNullJson()
    {
        string? obj = null;

        var json = _sut.Serialize(obj);

        json.Should().Be("null");
    }

    [Fact]
    public void Serialize_Collection_ReturnsJsonArray()
    {
        var obj = new[] { 1, 2, 3 };

        var json = _sut.Serialize(obj);

        json.Should().Be("[1,2,3]");
    }

    [Fact]
    public void Serialize_UseCamelCase()
    {
        var obj = new SampleDto { FirstName = "Alice", LastName = "Smith" };

        var json = _sut.Serialize(obj);

        json.Should().Contain("\"firstName\"");
        json.Should().Contain("\"lastName\"");
    }

    private class SampleDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
