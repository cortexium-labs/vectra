using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Vectra.Application.Abstractions.Executions;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Policy;

public class FileSystemPolicyLoader : IPolicyLoader
{
    private readonly string _policyDirectory;
    private readonly ILogger<FileSystemPolicyLoader> _logger;

    public FileSystemPolicyLoader(IConfiguration config, ILogger<FileSystemPolicyLoader> logger)
    {
        _policyDirectory = config["Policy:Directory"] ?? "policies";
        _logger = logger;
    }

    public async Task<Dictionary<Guid, PolicyDefinition>> LoadAllAsync(CancellationToken ct)
    {
        var policies = new Dictionary<Guid, PolicyDefinition>();
        if (!Directory.Exists(_policyDirectory))
        {
            _logger.LogWarning("Policy directory {Directory} does not exist", _policyDirectory);
            return policies;
        }

        foreach (var file in Directory.GetFiles(_policyDirectory, "*.json"))
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