using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Executions;
using Vectra.BuildingBlocks.Configuration.Semantic;
using Vectra.Infrastructure.Caches;

namespace Vectra.Infrastructure.Semantic.Providers.AzureAi;

public class AzureAiProvider : SemanticProviderBase, ISemanticProvider
{
    private readonly ChatCompletionsClient _client;
    private readonly AzureAiConfiguration _config;
    private readonly ICacheProvider _cacheProvider;
    private readonly ILogger<AzureAiProvider> _logger;



    public AzureAiProvider(
        IOptions<SemanticConfiguration> options,
        ICacheService cacheService,
        ILogger<AzureAiProvider> logger)
    {
        _config = options.Value.Providers.AzureAi;
        _client = new ChatCompletionsClient(
            new Uri(_config.Endpoint),
            new AzureKeyCredential(_config.ApiKey));
        _cacheProvider = cacheService.Current ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SemanticAnalysisResult> AnalyzeAsync(string? body, string metadata, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new SemanticAnalysisResult { Intent = "unknown", Confidence = 0.5, FallbackSafe = true };

        var cacheKey = $"semantic_azureai:{ComputeHash(body)}";
        var (success, cached) = await _cacheProvider.TryGetValueAsync<SemanticAnalysisResult>(cacheKey);
        if (success)
            return cached!;

        var requestOptions = new ChatCompletionsOptions
        {
            Model = _config.Model,
            Temperature = (float?)_config.Temperature,
            MaxTokens = _config.MaxTokens,
            Messages =
            {
                new ChatRequestSystemMessage(SystemPrompt),
                new ChatRequestUserMessage($"Metadata: {metadata}\n\nRequest body:\n{body}")
            }
        };

        SemanticAnalysisResult result;
        try
        {
            var response = await _client.CompleteAsync(requestOptions, cancellationToken);
            var content = response.Value.Content;
            result = ParseResponse(content, "AzureAI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AzureAI semantic provider failed; returning safe fallback");
            result = new SemanticAnalysisResult { Intent = "unknown", Confidence = 0.5, FallbackSafe = true };
        }

        await _cacheProvider.SetAsync(cacheKey, result);
        return result;
    }

}
