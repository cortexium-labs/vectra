using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vectra.Application.Abstractions.Executions;
using Vectra.BuildingBlocks.Configuration.Features;

namespace Vectra.Infrastructure.Policy.Providers;

public class OpaPolicyEngine : IPolicyProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<FeaturesConfiguration> _features;
    private readonly ILogger<OpaPolicyEngine> _logger;
    private const string OpaHttpClientName = "opa-policy";

    public OpaPolicyEngine(
        IHttpClientFactory httpClientFactory,
        IOptions<FeaturesConfiguration> features,
        ILogger<OpaPolicyEngine> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PolicyDecision> EvaluateAsync(
        string policyName, 
        Dictionary<string, object> input, 
        CancellationToken cancellationToken)
    {
        var opa = _features.Value.Policy?.Opa;
        if (opa is null || string.IsNullOrWhiteSpace(opa.BaseUrl))
            return PolicyDecision.Deny("OPA is selected but OPA base URL is not configured");

        var client = _httpClientFactory.CreateClient(OpaHttpClientName);
        client.BaseAddress = new Uri(opa.BaseUrl, UriKind.Absolute);
        client.Timeout = opa.Timeout ?? TimeSpan.FromSeconds(5);

        var body = new
        {
            input = new Dictionary<string, object>(input)
            {
                ["policyName"] = policyName
            }
        };

        using var response = await client.PostAsJsonAsync(opa.Path, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OPA returned status code {StatusCode}", response.StatusCode);
            return PolicyDecision.Deny($"OPA request failed with status code {(int)response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!json.RootElement.TryGetProperty("result", out var result))
            return PolicyDecision.Deny("OPA response does not contain 'result'");

        if (result.ValueKind == JsonValueKind.True) return PolicyDecision.Allow("OPA allow");
        if (result.ValueKind == JsonValueKind.False) return PolicyDecision.Deny("OPA deny");

        if (result.ValueKind == JsonValueKind.Object)
        {
            if (result.TryGetProperty("effect", out var effectProperty) &&
                effectProperty.ValueKind == JsonValueKind.String)
            {
                var effect = effectProperty.GetString();
                return effect?.ToLowerInvariant() switch
                {
                    "allow" => PolicyDecision.Allow(ReadReason(result)),
                    "hitl" => PolicyDecision.Hitl(ReadReason(result)),
                    _ => PolicyDecision.Deny(ReadReason(result))
                };
            }

            if (result.TryGetProperty("allow", out var allowProperty) && allowProperty.ValueKind == JsonValueKind.True)
                return PolicyDecision.Allow(ReadReason(result));

            if (result.TryGetProperty("hitl", out var hitlProperty) && hitlProperty.ValueKind == JsonValueKind.True)
                return PolicyDecision.Hitl(ReadReason(result));

            return PolicyDecision.Deny(ReadReason(result));
        }

        return PolicyDecision.Deny("Unsupported OPA result format");
    }

    private static string? ReadReason(JsonElement result)
        => result.TryGetProperty("reason", out var reason) && reason.ValueKind == JsonValueKind.String
            ? reason.GetString()
            : null;
}