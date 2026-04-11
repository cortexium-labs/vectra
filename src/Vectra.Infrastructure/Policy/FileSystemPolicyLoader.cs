using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Vectra.Application.Abstractions.Executions;
using Vectra.BuildingBlocks.Configuration.Features;
using Vectra.BuildingBlocks.Configuration.Features.Policy;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Policy;

public class FileSystemPolicyLoader : IPolicyLoader
{
    private readonly PolicyConfiguration _policyConfiguration;
    private readonly ILogger<FileSystemPolicyLoader> _logger;

    public FileSystemPolicyLoader(
        IOptions<FeaturesConfiguration> options, 
        ILogger<FileSystemPolicyLoader> logger)
    {
        _policyConfiguration = options.Value.Policy ?? new PolicyConfiguration();
        _logger = logger;
    }

    public async Task<PolicyDefinition?> GetPolicyAsync(Guid policyId, CancellationToken ct)
    {
        var allPolicies = await LoadAllAsync(ct);
        return allPolicies.TryGetValue(policyId, out var policy) ? policy : null;
    }

    public async Task<Dictionary<Guid, PolicyDefinition>> LoadAllAsync(CancellationToken ct)
    {
        var policies = new Dictionary<Guid, PolicyDefinition>();
        if (string.IsNullOrEmpty(_policyConfiguration.Directory))
        {
            _logger.LogWarning("Policy directory is not configured");
            return policies;
        }

        if (!Directory.Exists(_policyConfiguration.Directory))
        {
            _logger.LogWarning("Policy directory {Directory} does not exist", _policyConfiguration.Directory);
            return policies;
        }

        foreach (var file in Directory.GetFiles(_policyConfiguration.Directory, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var policy = JsonSerializer.Deserialize<PolicyDefinition>(json);
                if (policy != null && policy.Id != Guid.Empty)
                {
                    policies[policy.Id] = policy;
                    _logger.LogInformation("Loaded policy {PolicyName} ({PolicyId}) from {File}", policy.Name, policy.Id, file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load policy from {File}", file);
            }
        }
        return policies;
    }
}