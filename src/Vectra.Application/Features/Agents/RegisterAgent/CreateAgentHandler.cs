using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Abstractions.Security;
using Vectra.BuildingBlocks.Results;
using Vectra.Domain.Agents;

namespace Vectra.Application.Features.Agents.RegisterAgent;

internal class CreateAgentHandler : IActionHandler<CreateAgentRequest, Result<CreateAgentResult>>
{
    private readonly ILogger<CreateAgentHandler> _logger;
    private readonly IAgentRepository _agentRepository;
    private readonly ISecretHasher _secretHasher;

    public CreateAgentHandler(
        ILogger<CreateAgentHandler> logger,
        IAgentRepository agentRepository,
        ISecretHasher secretHasher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
        _secretHasher = secretHasher ?? throw new ArgumentNullException(nameof(secretHasher));
    }

    public async Task<Result<CreateAgentResult>> Handle(CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var clientSecretHash = _secretHasher.HashPassword(request.ClientSecret);
        var agent = new Agent(request.Name, request.OwnerId, clientSecretHash);
        await _agentRepository.AddAsync(agent, cancellationToken);

        return Result<CreateAgentResult>.Success(new CreateAgentResult { AgentId = agent.Id });
    }
}