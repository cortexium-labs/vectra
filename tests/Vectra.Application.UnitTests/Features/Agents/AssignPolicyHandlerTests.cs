using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Features.Agents.AssignPolicy;
using Vectra.Domain.Agents;
using Vectra.Domain.Policies;

namespace Vectra.Application.UnitTests.Features.Agents;

public class AssignPolicyHandlerTests
{
    private readonly ILogger<AssignPolicyHandler> _logger = Substitute.For<ILogger<AssignPolicyHandler>>();
    private readonly IAgentRepository _agentRepository = Substitute.For<IAgentRepository>();
    private readonly IPolicyLoader _policyLoader = Substitute.For<IPolicyLoader>();
    private readonly AssignPolicyHandler _sut;

    public AssignPolicyHandlerTests()
    {
        _sut = new AssignPolicyHandler(_logger, _agentRepository, _policyLoader);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAgentAndPolicyExist()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = new Agent("Agent1", "owner-1", "hash");
        var policy = new PolicyDefinition { Name = "DefaultPolicy", Owner = "team" };

        _agentRepository.GetByIdAsync(Arg.Any<Guid>(), CancellationToken.None).Returns(agent);
        _policyLoader.GetPolicyAsync("DefaultPolicy", CancellationToken.None).Returns(policy);

        var request = new AssignPolicyRequest
        {
            AgentId = agentId.ToString(),
            PolicyName = "DefaultPolicy"
        };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        agent.PolicyName.Should().Be("DefaultPolicy");
        await _agentRepository.Received(1).UpdateAsync(agent, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAgentNotFound()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _agentRepository.GetByIdAsync(Arg.Any<Guid>(), CancellationToken.None).Returns((Agent?)null);

        var request = new AssignPolicyRequest
        {
            AgentId = agentId.ToString(),
            PolicyName = "AnyPolicy"
        };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        await _agentRepository.DidNotReceive().UpdateAsync(Arg.Any<Agent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPolicyNotFound()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = new Agent("Agent1", "owner-1", "hash");

        _agentRepository.GetByIdAsync(Arg.Any<Guid>(), CancellationToken.None).Returns(agent);
        _policyLoader.GetPolicyAsync(Arg.Any<string>(), CancellationToken.None).Returns((PolicyDefinition?)null);

        var request = new AssignPolicyRequest
        {
            AgentId = agentId.ToString(),
            PolicyName = "NonExistentPolicy"
        };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        await _agentRepository.DidNotReceive().UpdateAsync(Arg.Any<Agent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        var act = () => new AssignPolicyHandler(null!, _agentRepository, _policyLoader);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
    {
        var act = () => new AssignPolicyHandler(_logger, null!, _policyLoader);
        act.Should().Throw<ArgumentNullException>().WithParameterName("agentRepository");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPolicyLoaderIsNull()
    {
        var act = () => new AssignPolicyHandler(_logger, _agentRepository, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("policyLoader");
    }
}
