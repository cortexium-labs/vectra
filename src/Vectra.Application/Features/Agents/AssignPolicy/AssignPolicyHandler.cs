using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Errors;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Agents.AssignPolicy;

internal class AssignPolicyHandler : IActionHandler<AssignPolicyRequest, Result<Abstractions.Dispatchers.Void>>
{
    private readonly ILogger<AssignPolicyHandler> _logger;
    private readonly IAgentRepository _agentRepository;
    private readonly IPolicyLoader _policyLoader;

    public AssignPolicyHandler(
        ILogger<AssignPolicyHandler> logger,
        IAgentRepository agentRepository,
        IPolicyLoader policyLoader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
        _policyLoader = policyLoader ?? throw new ArgumentNullException(nameof(policyLoader));
    }

    public async Task<Result<Abstractions.Dispatchers.Void>> Handle(AssignPolicyRequest request, CancellationToken cancellationToken)
    {
        var agentId = Guid.Parse(request.AgentId);
        var agent = await _agentRepository.GetByIdAsync(agentId, cancellationToken);
        if (agent == null)
        {
            _logger.LogWarning("Agent with ID {AgentId} not found.", request.AgentId);
            var error = Error.NotFound(ApplicationErrorCodes.AgentNotFound, $"Agent with ID {request.AgentId} not found.");
            return Result<Abstractions.Dispatchers.Void>.Failure(error);
        }

        var policyId = Guid.Parse(request.PolicyId);
        var policy = await _policyLoader.GetPolicyAsync(policyId, cancellationToken);
        if (policy == null)
        {
            _logger.LogWarning("Policy with ID {PolicyId} not found.", request.PolicyId);
            var error = Error.NotFound(ApplicationErrorCodes.PolicyNotFound, $"Policy with ID {request.PolicyId} not found.");
            return Result<Abstractions.Dispatchers.Void>.Failure(error);
        }

        agent.PolicyId = policyId;
        await _agentRepository.UpdateAsync(agent, cancellationToken);
        return Result<Abstractions.Dispatchers.Void>.Success(new Abstractions.Dispatchers.Void());
    }
}