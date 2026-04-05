using Vectra.Application.Abstractions.Dispatchers;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.Features.Authentications.GenerateToken;

public class GenerateTokenRequest : IRequest<Result<GenerateTokenResult>>
{
    public Guid AgentId { get; set; }
    public string ClientSecret { get; set; } = string.Empty;
}
