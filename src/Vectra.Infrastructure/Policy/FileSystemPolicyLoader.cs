using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Serializations;
using Vectra.BuildingBlocks.Configuration.Policy;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Policy;

public class FileSystemPolicyLoader : IPolicyLoader
{
    private readonly IOptions<PolicyConfiguration> _options;
    private readonly ILogger<FileSystemPolicyLoader> _logger;
    private readonly IDeserializer _deserializer;

    public FileSystemPolicyLoader(
        IOptions<PolicyConfiguration> options, 
        ILogger<FileSystemPolicyLoader> logger,
        IDeserializer deserializer)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    }

    public async Task<PolicyDefinition?> GetPolicyAsync(string policyName, CancellationToken ct)
    {
        var allPolicies = await LoadAllAsync(ct);
        return allPolicies.TryGetValue(policyName, out var policy) ? policy : null;
    }

    public async Task<Dictionary<string, PolicyDefinition>> LoadAllAsync(CancellationToken ct)
    {
        var policyConfiguration = _options.Value.Providers.Internal;
        var policies = new Dictionary<string, PolicyDefinition>();
        var configuredPath = policyConfiguration.Directory;

        if (string.IsNullOrEmpty(configuredPath))
        {
            _logger.LogWarning("Policy directory is not configured");
            return policies;
        }

        string fullPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));

        if (!Directory.Exists(fullPath))
        {
            _logger.LogWarning("Policy directory {Directory} does not exist", fullPath);
            return policies;
        }

        foreach (var file in Directory.GetFiles(fullPath, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var policy = _deserializer.Deserialize<PolicyDefinition>(json);
                if (policy != null && !string.IsNullOrEmpty(policy.Name))
                {
                    policies[policy.Name] = policy;
                    _logger.LogInformation("Loaded policy {PolicyName} from {File}", policy.Name, file);
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