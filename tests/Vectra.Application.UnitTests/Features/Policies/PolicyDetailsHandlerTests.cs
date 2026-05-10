using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Features.Policies.PolicyDetails;
using Vectra.Domain.Policies;

namespace Vectra.Application.UnitTests.Features.Policies;

public class PolicyDetailsHandlerTests
{
    private readonly ILogger<PolicyDetailsHandler> _logger = Substitute.For<ILogger<PolicyDetailsHandler>>();
    private readonly IPolicyCacheService _policyCacheService = Substitute.For<IPolicyCacheService>();
    private readonly PolicyDetailsHandler _sut;

    public PolicyDetailsHandlerTests()
    {
        _sut = new PolicyDetailsHandler(_logger, _policyCacheService);
    }

    [Fact]
    public async Task Handle_ShouldReturnPolicyDetails_WhenPolicyExists()
    {
        // Arrange
        var policy = new PolicyDefinition
        {
            Name = "SecurityPolicy",
            Description = "Handles security",
            Owner = "security-team",
            CreatedOn = new DateTime(2024, 1, 1),
            Default = PolicyType.Deny,
            Rules = []
        };
        _policyCacheService.GetPagedAsync(1, int.MaxValue, CancellationToken.None)
            .Returns((new List<PolicyDefinition> { policy }.AsReadOnly() as IReadOnlyList<PolicyDefinition>, 1));

        var request = new PolicyDetailsRequest { Name = "SecurityPolicy" };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("SecurityPolicy");
        result.Value.Description.Should().Be("Handles security");
        result.Value.Owner.Should().Be("security-team");
        result.Value.CreatedOn.Should().Be(new DateTime(2024, 1, 1));
        result.Value.Default.Should().Be(PolicyType.Deny);
    }

    [Fact]
    public async Task Handle_ShouldBeCaseInsensitive_WhenMatchingPolicyName()
    {
        // Arrange
        var policy = new PolicyDefinition { Name = "SecurityPolicy", Owner = "team" };
        _policyCacheService.GetPagedAsync(1, int.MaxValue, CancellationToken.None)
            .Returns((new List<PolicyDefinition> { policy }.AsReadOnly() as IReadOnlyList<PolicyDefinition>, 1));

        var request = new PolicyDetailsRequest { Name = "securitypolicy" };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("SecurityPolicy");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPolicyNotFound()
    {
        // Arrange
        _policyCacheService.GetPagedAsync(1, int.MaxValue, CancellationToken.None)
            .Returns((new List<PolicyDefinition>().AsReadOnly() as IReadOnlyList<PolicyDefinition>, 0));

        var request = new PolicyDetailsRequest { Name = "NonExistent" };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        var act = () => new PolicyDetailsHandler(null!, _policyCacheService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPolicyCacheServiceIsNull()
    {
        var act = () => new PolicyDetailsHandler(_logger, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("policyCacheService");
    }
}
