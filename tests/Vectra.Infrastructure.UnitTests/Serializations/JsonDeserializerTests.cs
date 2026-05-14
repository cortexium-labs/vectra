using FluentAssertions;
using Vectra.Infrastructure.Serializations.Json;
using Vectra.Infrastructure.Exceptions;

namespace Vectra.Infrastructure.UnitTests.Serializations;

public class JsonDeserializerTests
{
    private readonly JsonDeserializer _sut = new();

    [Fact]
    public void Deserialize_ValidJson_ReturnsObject()
    {
        var json = "{\"firstName\":\"Alice\",\"lastName\":\"Smith\"}";

        var result = _sut.Deserialize<SampleDto>(json);

        result.Should().NotBeNull();
        result.FirstName.Should().Be("Alice");
        result.LastName.Should().Be("Smith");
    }

    [Fact]
    public void Deserialize_NullInput_ThrowsJsonSerializationInputRequiredException()
    {
        var act = () => _sut.Deserialize<SampleDto>(null);

        act.Should().Throw<JsonSerializationInputRequiredException>();
    }

    [Fact]
    public void Deserialize_EmptyInput_ThrowsJsonSerializationInputRequiredException()
    {
        var act = () => _sut.Deserialize<SampleDto>(string.Empty);

        act.Should().Throw<JsonSerializationInputRequiredException>();
    }

    [Fact]
    public void Deserialize_WhitespaceInput_ThrowsJsonSerializationInputRequiredException()
    {
        var act = () => _sut.Deserialize<SampleDto>("   ");

        act.Should().Throw<JsonSerializationInputRequiredException>();
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsJsonSerializationException()
    {
        var act = () => _sut.Deserialize<SampleDto>("{not valid json}");

        act.Should().Throw<JsonSerializationException>();
    }

    [Fact]
    public void Deserialize_CaseInsensitive_MapsProperties()
    {
        var json = "{\"FIRSTNAME\":\"Bob\",\"LASTNAME\":\"Jones\"}";

        var result = _sut.Deserialize<SampleDto>(json);

        result.FirstName.Should().Be("Bob");
        result.LastName.Should().Be("Jones");
    }

    [Fact]
    public async Task DeserializeDynamicAsync_ValidJson_ReturnsJsonElement()
    {
        var json = "{\"key\":\"value\"}";

        var result = await _sut.DeserializeDynamicAsync(json);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DeserializeDynamicAsync_InvalidJson_ThrowsJsonSerializationException()
    {
        var act = async () => await _sut.DeserializeDynamicAsync("{bad}");

        await act.Should().ThrowAsync<JsonSerializationException>();
    }

    [Fact]
    public async Task TryDeserializeAsync_ValidJson_ReturnsTrueAndResult()
    {
        var json = "{\"firstName\":\"Alice\",\"lastName\":\"Smith\"}";

        var success = await _sut.TryDeserializeAsync<SampleDto>(json, out var result);

        success.Should().BeTrue();
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task TryDeserializeAsync_InvalidJson_ReturnsFalse()
    {
        var success = await _sut.TryDeserializeAsync<SampleDto>("{bad json}", out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }

    private class SampleDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
