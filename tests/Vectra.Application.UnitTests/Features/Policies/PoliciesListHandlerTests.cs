using FluentAssertions;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Features.Policies.PoliciesList;
using Vectra.Domain.Policies;

namespace Vectra.Application.UnitTests.Features.Policies;

public class PoliciesListHandlerTests
{
    private readonly IPolicyCacheService _policyCacheService = Substitute.For<IPolicyCacheService>();
    private readonly PoliciesListHandler _sut;

    public PoliciesListHandlerTests()
    {
        _sut = new PoliciesListHandler(_policyCacheService);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedSuccess_WithMappedPolicies()
    {
        // Arrange
        var policies = new List<PolicyDefinition>
        {
            new() { Name = "Policy1", Description = "Desc1", Owner = "team-a" },
            new() { Name = "Policy2", Description = "Desc2", Owner = "team-b" }
        };
        _policyCacheService.GetPagedAsync(1, 25, CancellationToken.None)
            .Returns((policies.AsReadOnly() as IReadOnlyList<PolicyDefinition>, 2));

        var request = new PoliciesListRequest { Page = 1, PageSize = 25 };

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
    public async Task Handle_ShouldMapPolicyPropertiesCorrectly()
    {
        // Arrange
        var policy = new PolicyDefinition { Name = "MyPolicy", Description = "A desc", Owner = "owner-team" };
        _policyCacheService.GetPagedAsync(1, 25, CancellationToken.None)
            .Returns((new List<PolicyDefinition> { policy }.AsReadOnly() as IReadOnlyList<PolicyDefinition>, 1));

        var request = new PoliciesListRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        var item = result.Items.Single();
        item.PolicyName.Should().Be("MyPolicy");
        item.Description.Should().Be("A desc");
        item.Owner.Should().Be("owner-team");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoPoliciesExist()
    {
        // Arrange
        _policyCacheService.GetPagedAsync(1, 25, CancellationToken.None)
            .Returns((new List<PolicyDefinition>().AsReadOnly() as IReadOnlyList<PolicyDefinition>, 0));

        var request = new PoliciesListRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldMapNullDescriptionToEmptyString()
    {
        // Arrange
        var policy = new PolicyDefinition { Name = "NoDescPolicy", Description = null, Owner = "owner" };
        _policyCacheService.GetPagedAsync(1, 25, CancellationToken.None)
            .Returns((new List<PolicyDefinition> { policy }.AsReadOnly() as IReadOnlyList<PolicyDefinition>, 1));

        var request = new PoliciesListRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.Items.Single().Description.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPolicyCacheServiceIsNull()
    {
        var act = () => new PoliciesListHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("policyCacheService");
    }
}
