using FluentAssertions;
using Vectra.Application.Abstractions.Serializations;

namespace Vectra.Application.UnitTests.Abstractions.Serializations;

public class SerializationConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var config = new SerializationConfiguration();

        config.Indented.Should().BeFalse();
        config.NameCaseInsensitive.Should().BeTrue();
        config.Converters.Should().BeNull();
    }

    [Fact]
    public void SetProperties_ShouldPersistValues()
    {
        var converters = new List<object> { new object() };
        var config = new SerializationConfiguration
        {
            Indented = true,
            NameCaseInsensitive = false,
            Converters = converters
        };

        config.Indented.Should().BeTrue();
        config.NameCaseInsensitive.Should().BeFalse();
        config.Converters.Should().BeSameAs(converters);
    }
}
