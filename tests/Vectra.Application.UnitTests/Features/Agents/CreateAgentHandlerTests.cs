using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Abstractions.Security;
using Vectra.Application.Features.Agents.RegisterAgent;

namespace Vectra.Application.UnitTests.Features.Agents;

public class CreateAgentHandlerTests
{
    private readonly ILogger<CreateAgentHandler> _logger = Substitute.For<ILogger<CreateAgentHandler>>();
    private readonly IAgentRepository _agentRepository = Substitute.For<IAgentRepository>();
    private readonly ISecretHasher _secretHasher = Substitute.For<ISecretHasher>();
    private readonly CreateAgentHandler _sut;

    public CreateAgentHandlerTests()
    {
        _sut = new CreateAgentHandler(_logger, _agentRepository, _secretHasher);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAgentIsCreated()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "TestAgent",
            OwnerId = "owner-1",
            ClientSecret = "s3cr3t"
        };
        _secretHasher.HashPassword(request.ClientSecret).Returns("hashed-secret");

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AgentId.Should().NotBeEmpty();
        await _agentRepository.Received(1).AddAsync(Arg.Any<Vectra.Domain.Agents.Agent>(), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ShouldHashClientSecret_BeforeStoringAgent()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "TestAgent",
            OwnerId = "owner-1",
            ClientSecret = "plainSecret"
        };
        _secretHasher.HashPassword("plainSecret").Returns("hashedValue");

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        _secretHasher.Received(1).HashPassword("plainSecret");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new CreateAgentHandler(null!, _agentRepository, _secretHasher);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
    {
        // Act
        var act = () => new CreateAgentHandler(_logger, null!, _secretHasher);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("agentRepository");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSecretHasherIsNull()
    {
        // Act
        var act = () => new CreateAgentHandler(_logger, _agentRepository, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("secretHasher");
    }
}
