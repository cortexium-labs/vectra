using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Executions;
using Vectra.BuildingBlocks.Configuration.Semantic;
using Vectra.Infrastructure.Caches;

namespace Vectra.Infrastructure.Semantic.Providers.AzureAi;

public class AzureAiProvider : ISemanticProvider
{
    private readonly ChatCompletionsClient _client;
    private readonly AzureAiConfiguration _config;
    private readonly ICacheProvider _cacheProvider;
    private readonly ILogger<AzureAiProvider> _logger;

    private const string SystemPrompt =
        """
        You are a security intent classifier. Given an HTTP request body, classify the intent into one of:
        bulk_export, destructive_delete, admin_action, harmful, read, write, unknown.
        Respond with a JSON object only, no markdown, in this exact format:
        {"intent":"<label>","confidence":<0.0-1.0>,"risk_tags":["tag1"],"explanation":"<short>"}
        Risk tags: use data_exfiltration, destructive, privilege_escalation, malicious, or empty array.
        """;

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

    public async Task<SemanticAnalysisResult> AnalyzeAsync(string? requestBody, string metadata, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestBody))
            return new SemanticAnalysisResult { Intent = "unknown", Confidence = 0.5, FallbackSafe = true };

        var cacheKey = $"semantic_azureai:{ComputeHash(requestBody)}";
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
                new ChatRequestUserMessage($"Metadata: {metadata}\n\nRequest body:\n{requestBody}")
            }
        };

        SemanticAnalysisResult result;
        try
        {
            var response = await _client.CompleteAsync(requestOptions, cancellationToken);
            var content = response.Value.Content;
            result = ParseResponse(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AzureAI semantic provider failed; returning safe fallback");
            result = new SemanticAnalysisResult { Intent = "unknown", Confidence = 0.5, FallbackSafe = true };
        }

        await _cacheProvider.SetAsync(cacheKey, result);
        return result;
    }

    private static SemanticAnalysisResult ParseResponse(string content)
    {
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;
            var intent = root.GetProperty("intent").GetString() ?? "unknown";
            var confidence = root.GetProperty("confidence").GetDouble();
            var explanation = root.TryGetProperty("explanation", out var exp) ? exp.GetString() : null;
            var riskTags = root.TryGetProperty("risk_tags", out var tags)
                ? tags.EnumerateArray().Select(t => t.GetString()!).ToArray()
                : Array.Empty<string>();

            return new SemanticAnalysisResult
            {
                Intent = intent,
                Confidence = confidence,
                RiskTags = riskTags,
                FallbackSafe = confidence < 0.7,
                Explanation = explanation ?? $"AzureAI: {intent} ({confidence:F2})"
            };
        }
        catch
        {
            return new SemanticAnalysisResult { Intent = "unknown", Confidence = 0.5, FallbackSafe = true };
        }
    }

    private static string ComputeHash(string input) =>
        Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(input)));
}
