using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Errors;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Agents.AssignPolicy;

internal class AssignPolicyHandler : IActionHandler<AssignPolicyRequest, Result<Abstractions.Dispatchers.Void>>
{
    private readonly ILogger<AssignPolicyHandler> _logger;
    private readonly IAgentRepository _agentRepository;

    public AssignPolicyHandler(
        ILogger<AssignPolicyHandler> logger,
        IAgentRepository agentRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
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
        agent.PolicyId = policyId;
        await _agentRepository.UpdateAsync(agent, cancellationToken);
        return Result<Abstractions.Dispatchers.Void>.Success(new Abstractions.Dispatchers.Void());
    }
}