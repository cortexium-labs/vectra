using FluentAssertions;
using Vectra.Infrastructure.Decision;

namespace Vectra.Infrastructure.UnitTests.Decision;

public class JsonToIntentTextTests
{
    [Fact]
    public void Convert_SimpleObject_ReturnsKeyValueText()
    {
        var json = "{\"method\":\"POST\",\"path\":\"/api/data\"}";

        var result = JsonToIntentText.Convert(json);

        result.Should().Contain("method");
        result.Should().Contain("post");
        result.Should().Contain("path");
        result.Should().Contain("/api/data");
    }

    [Fact]
    public void Convert_CamelCaseKeys_AreNormalized()
    {
        var json = "{\"agentName\":\"MyAgent\"}";

        var result = JsonToIntentText.Convert(json);

        result.Should().Contain("agent name");
    }

    [Fact]
    public void Convert_SnakeCaseKeys_AreNormalized()
    {
        var json = "{\"agent_name\":\"MyAgent\"}";

        var result = JsonToIntentText.Convert(json);

        result.Should().Contain("agent name");
    }

    [Fact]
    public void Convert_NoiseFields_AreFiltered()
    {
        var json = "{\"id\":\"some-id\",\"method\":\"GET\"}";

        var result = JsonToIntentText.Convert(json);

        result.Should().NotContain("id some-id");
        result.Should().Contain("method");
    }

    [Fact]
    public void Convert_ArrayValues_AreJoined()
    {
        var json = "{\"roles\":[\"admin\",\"user\"]}";

        var result = JsonToIntentText.Convert(json);

        result.Should().Contain("admin");
        result.Should().Contain("user");
    }

    [Fact]
    public void Convert_BooleanValue_IsIncluded()
    {
        var json = "{\"active\":true}";

        var result = JsonToIntentText.Convert(json);

        result.Should().Contain("true");
    }

    [Fact]
    public void Convert_NumericValue_IsIncluded()
    {
        var json = "{\"count\":42}";

        var result = JsonToIntentText.Convert(json);

        result.Should().Contain("42");
    }

    [Fact]
    public void Convert_NestedObject_ProcessesDeep()
    {
        var json = "{\"user\":{\"role\":\"admin\"}}";

        var result = JsonToIntentText.Convert(json);

        result.Should().Contain("role");
        result.Should().Contain("admin");
    }

    [Fact]
    public void Convert_EmptyObject_ReturnsEmptyOrWhitespace()
    {
        var json = "{}";

        var result = JsonToIntentText.Convert(json);

        result.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Convert_TimestampNoiseFields_AreFiltered()
    {
        var json = "{\"timestamp\":\"2024-01-01\",\"action\":\"login\"}";

        var result = JsonToIntentText.Convert(json);

        result.Should().NotContain("2024-01-01");
        result.Should().Contain("action");
        result.Should().Contain("login");
    }

    [Fact]
    public void Convert_TrailingCommasAllowed()
    {
        var json = "{\"method\":\"GET\",}";

        var act = () => JsonToIntentText.Convert(json);

        act.Should().NotThrow();
    }

    [Fact]
    public void Convert_ValuesLowercased()
    {
        var json = "{\"method\":\"DELETE\"}";

        var result = JsonToIntentText.Convert(json);

        result.Should().Contain("delete");
    }
}
