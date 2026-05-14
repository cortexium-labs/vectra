using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Features.Agents.DeleteAgent;

namespace Vectra.Application.UnitTests.Features.Agents;

public class DeleteAgentHandlerTests
{
    private readonly ILogger<DeleteAgentHandler> _logger = Substitute.For<ILogger<DeleteAgentHandler>>();
    private readonly IAgentRepository _agentRepository = Substitute.For<IAgentRepository>();
    private readonly DeleteAgentHandler _sut;

    public DeleteAgentHandlerTests()
    {
        _sut = new DeleteAgentHandler(_agentRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAgentIsDeleted()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var request = new DeleteAgentRequest { AgentId = agentId.ToString() };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _agentRepository.Received(1).DeleteAsync(agentId, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ShouldCallDeleteWithParsedGuid()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var request = new DeleteAgentRequest { AgentId = agentId.ToString() };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _agentRepository.Received(1).DeleteAsync(Arg.Is<Guid>(id => id == agentId), CancellationToken.None);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        var act = () => new DeleteAgentHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("agentRepository");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
    {
        var act = () => new DeleteAgentHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("agentRepository");
    }
}
