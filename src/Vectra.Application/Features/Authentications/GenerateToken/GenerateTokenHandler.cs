using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Errors;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;
using Vectra.Domain.Agents;

namespace Vectra.Application.Features.Authentications.GenerateToken;

internal class GenerateTokenHandler : IActionHandler<GenerateTokenRequest, Result<GenerateTokenResult>>
{
    private readonly ILogger<GenerateTokenHandler> _logger;
    private readonly IAgentRepository _agentRepository;
    private readonly ITokenService _tokenService;

    public GenerateTokenHandler(
        ILogger<GenerateTokenHandler> logger,
        IAgentRepository agentRepository,
        ITokenService tokenService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    public async Task<Result<GenerateTokenResult>> Handle(GenerateTokenRequest request, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByIdAsync(request.AgentId, cancellationToken);
        if (agent == null || agent.Status != AgentStatus.Active)
            return Result<GenerateTokenResult>.Failure(Error.NotFound(
                ApplicationErrorCodes.AgentNotFound, "Agent not found or inactive"));

        if (!BCrypt.Net.BCrypt.Verify(request.ClientSecret, agent.ClientSecretHash))
            return Result<GenerateTokenResult>.Failure(Error.Unauthorized(
                ApplicationErrorCodes.InvalidClientSession, "Invalid client secret"));

        var token = _tokenService.GenerateToken(agent);
        return Result<GenerateTokenResult>.Success(new GenerateTokenResult { AccessToken = token });
    }
}