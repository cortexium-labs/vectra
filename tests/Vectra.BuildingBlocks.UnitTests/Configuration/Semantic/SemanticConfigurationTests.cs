using FluentAssertions;
using Vectra.BuildingBlocks.Configuration.Semantic;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Configuration.Semantic;

public class SemanticConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var config = new SemanticConfiguration();

        config.Enabled.Should().BeFalse();
        config.ConfidenceThreshold.Should().Be(0.7);
        config.AllowLowConfidence.Should().BeFalse();
        config.DefaultProvider.Should().Be("Internal");
        config.Providers.Should().NotBeNull();
    }

    [Fact]
    public void SemanticProviders_ShouldInitializeAllProviders()
    {
        var providers = new SemanticProviders();

        providers.Internal.Should().NotBeNull();
        providers.AzureAi.Should().NotBeNull();
        providers.OpenAi.Should().NotBeNull();
        providers.Gemini.Should().NotBeNull();
        providers.Ollama.Should().NotBeNull();
    }

    [Fact]
    public void InternalOnnxConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new InternalOnnxConfiguration();

        config.ModelType.Should().Be("Community");
        config.PackagePath.Should().BeNull();
        config.LicensePath.Should().BeNull();
        config.MaxLength.Should().Be(128);
    }

    [Fact]
    public void AzureAiConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new AzureAiConfiguration();

        config.Endpoint.Should().BeEmpty();
        config.ApiKey.Should().BeEmpty();
        config.Model.Should().Be("gpt-4o-mini");
        config.Temperature.Should().Be(0.0);
        config.MaxTokens.Should().Be(256);
    }

    [Fact]
    public void OpenAiConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new OpenAiConfiguration();

        config.ApiKey.Should().BeEmpty();
        config.Model.Should().Be("gpt-4o-mini");
        config.Temperature.Should().Be(0.0);
        config.MaxTokens.Should().Be(256);
    }

    [Fact]
    public void GeminiConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new GeminiConfiguration();

        config.ApiKey.Should().BeEmpty();
        config.Model.Should().Be("gemini-2.0-flash");
        config.Temperature.Should().Be(0.0);
        config.MaxTokens.Should().Be(256);
    }

    [Fact]
    public void OllamaConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new OllamaConfiguration();

        config.Endpoint.Should().Be("http://localhost:11434");
        config.Model.Should().Be("llama3.2");
    }

    [Fact]
    public void AzureAiConfiguration_ShouldAllowCustomValues()
    {
        var config = new AzureAiConfiguration
        {
            Endpoint = "https://my.openai.azure.com/",
            ApiKey = "secret",
            Model = "gpt-4",
            Temperature = 0.5,
            MaxTokens = 512
        };

        config.Endpoint.Should().Be("https://my.openai.azure.com/");
        config.ApiKey.Should().Be("secret");
        config.Model.Should().Be("gpt-4");
        config.Temperature.Should().Be(0.5);
        config.MaxTokens.Should().Be(512);
    }
}
