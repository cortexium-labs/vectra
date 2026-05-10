using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Abstractions.Security;
using Vectra.Application.Features.Authentications.GenerateToken;
using Vectra.Domain.Agents;

namespace Vectra.Application.UnitTests.Features.Authentications;

public class GenerateTokenHandlerTests
{
    private readonly ILogger<GenerateTokenHandler> _logger = Substitute.For<ILogger<GenerateTokenHandler>>();
    private readonly IAgentRepository _agentRepository = Substitute.For<IAgentRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ISecretHasher _secretHasher = Substitute.For<ISecretHasher>();
    private readonly GenerateTokenHandler _sut;

    public GenerateTokenHandlerTests()
    {
        _sut = new GenerateTokenHandler(_logger, _agentRepository, _tokenService, _secretHasher);
    }

    [Fact]
    public async Task Handle_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = new Agent("TestAgent", "owner-1", "hashed-secret");
        var request = new GenerateTokenRequest { AgentId = agentId, ClientSecret = "plainSecret" };

        _agentRepository.GetByIdAsync(agentId, CancellationToken.None).Returns(agent);
        _secretHasher.Verify("plainSecret", agent.ClientSecretHash).Returns(true);
        _tokenService.GenerateToken(agent).Returns("jwt-token");

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("jwt-token");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAgentNotFound()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _agentRepository.GetByIdAsync(agentId, CancellationToken.None).Returns((Agent?)null);

        var request = new GenerateTokenRequest { AgentId = agentId, ClientSecret = "secret" };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _tokenService.DidNotReceive().GenerateToken(Arg.Any<Agent>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAgentIsRevoked()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = new Agent("TestAgent", "owner-1", "hashed-secret");
        agent.Revoke();

        _agentRepository.GetByIdAsync(agentId, CancellationToken.None).Returns(agent);

        var request = new GenerateTokenRequest { AgentId = agentId, ClientSecret = "secret" };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _tokenService.DidNotReceive().GenerateToken(Arg.Any<Agent>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenClientSecretIsInvalid()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = new Agent("TestAgent", "owner-1", "hashed-secret");

        _agentRepository.GetByIdAsync(agentId, CancellationToken.None).Returns(agent);
        _secretHasher.Verify("wrongSecret", agent.ClientSecretHash).Returns(false);

        var request = new GenerateTokenRequest { AgentId = agentId, ClientSecret = "wrongSecret" };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _tokenService.DidNotReceive().GenerateToken(Arg.Any<Agent>());
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        var act = () => new GenerateTokenHandler(null!, _agentRepository, _tokenService, _secretHasher);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
    {
        var act = () => new GenerateTokenHandler(_logger, null!, _tokenService, _secretHasher);
        act.Should().Throw<ArgumentNullException>().WithParameterName("agentRepository");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenTokenServiceIsNull()
    {
        var act = () => new GenerateTokenHandler(_logger, _agentRepository, null!, _secretHasher);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tokenService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSecretHasherIsNull()
    {
        var act = () => new GenerateTokenHandler(_logger, _agentRepository, _tokenService, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("secretHasher");
    }
}
