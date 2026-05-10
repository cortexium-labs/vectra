using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Features.Agents.AgentsList;
using Vectra.Domain.Agents;

namespace Vectra.Application.UnitTests.Features.Agents;

public class AgentsListHandlerTests
{
    private readonly ILogger<AgentsListHandler> _logger = Substitute.For<ILogger<AgentsListHandler>>();
    private readonly IAgentRepository _agentRepository = Substitute.For<IAgentRepository>();
    private readonly AgentsListHandler _sut;

    public AgentsListHandlerTests()
    {
        _sut = new AgentsListHandler(_logger, _agentRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedSuccess_WithMappedAgents()
    {
        // Arrange
        var agents = new List<Agent>
        {
            new("Agent1", "owner-1", "hash1"),
            new("Agent2", "owner-2", "hash2")
        };
        _agentRepository.GetPagedAsync(1, 25, CancellationToken.None)
            .Returns((agents.AsReadOnly() as IReadOnlyList<Agent>, 2));

        var request = new AgentsListRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task Handle_ShouldMapAgentPropertiesCorrectly()
    {
        // Arrange
        var agent = new Agent("MyAgent", "owner-99", "hashed");
        _agentRepository.GetPagedAsync(1, 25, CancellationToken.None)
            .Returns((new List<Agent> { agent }.AsReadOnly() as IReadOnlyList<Agent>, 1));

        var request = new AgentsListRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        var item = result.Items.Single();
        item.Name.Should().Be("MyAgent");
        item.OwnerId.Should().Be("owner-99");
        item.Status.Should().Be(AgentStatus.Active);
        item.AgentId.Should().Be(agent.Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoAgentsExist()
    {
        // Arrange
        _agentRepository.GetPagedAsync(1, 25, CancellationToken.None)
            .Returns((new List<Agent>().AsReadOnly() as IReadOnlyList<Agent>, 0));

        var request = new AgentsListRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _agentRepository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<(IReadOnlyList<Agent>, int)>(x => throw new OperationCanceledException(x.Arg<CancellationToken>()));

        var request = new AgentsListRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.Handle(request, cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        var act = () => new AgentsListHandler(null!, _agentRepository);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
    {
        var act = () => new AgentsListHandler(_logger, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("agentRepository");
    }
}
