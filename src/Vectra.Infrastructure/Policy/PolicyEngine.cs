using System.Net.Http.Json;
using Vectra.Core.Interfaces;

namespace Vectra.Infrastructure.Policy;

public class PolicyEngine : IPolicyEngine
{
    private readonly HttpClient _httpClient;

    public PolicyEngine(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OpaDecision> EvaluateAsync(string package, object input, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"v1/data/{package}", new { input }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OpaResponse>(cancellationToken: cancellationToken);
        return new OpaDecision(result?.Result?.Decision ?? "deny");
    }

    private class OpaResponse
    {
        public OpaResult Result { get; set; } = new();
    }

    private class OpaResult
    {
        public string Decision { get; set; } = string.Empty;
    }
}
