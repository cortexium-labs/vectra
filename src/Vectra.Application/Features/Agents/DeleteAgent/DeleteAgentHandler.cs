using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Persistence;
using Vectra.BuildingBlocks.Results;
using Void = Vectra.Application.Abstractions.Dispatchers.Void;

namespace Vectra.Application.Features.Agents.DeleteAgent;

internal class DeleteAgentHandler : IActionHandler<DeleteAgentRequest, Result<Void>>
{
    private readonly IAgentRepository _agentRepository;

    public DeleteAgentHandler(IAgentRepository agentRepository)
    {
        _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
    }

    public async Task<Result<Void>> Handle(DeleteAgentRequest request, CancellationToken cancellationToken)
    {
        var agentId = Guid.Parse(request.AgentId);
        await _agentRepository.DeleteAsync(agentId, cancellationToken);
        return await Result<Void>.SuccessAsync(new Void());
    }
}