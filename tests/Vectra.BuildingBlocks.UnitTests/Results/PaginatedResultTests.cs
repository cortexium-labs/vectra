using FluentAssertions;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Results;

public class PaginatedResultTests
{
    private static readonly IReadOnlyList<string> SampleItems = ["a", "b", "c"];

    [Fact]
    public void Success_ShouldReturnSuccessResultWithCorrectProperties()
    {
        var result = PaginatedResult<string>.Success(SampleItems, pageNumber: 1, pageSize: 10, totalCount: 3);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Items.Should().BeEquivalentTo(SampleItems);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task SuccessAsync_ShouldReturnSuccessResultWithCorrectProperties()
    {
        var result = await PaginatedResult<string>.SuccessAsync(SampleItems, pageNumber: 2, pageSize: 5, totalCount: 12);

        result.IsSuccess.Should().BeTrue();
        result.Items.Should().BeEquivalentTo(SampleItems);
        result.PageNumber.Should().Be(2);
    }

    [Fact]
    public void Failure_ShouldReturnFailureResultWithEmptyItems()
    {
        var error = Error.Failure(VectraErrors.SystemFailure, "Failure");

        var result = PaginatedResult<string>.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task FailureAsync_ShouldReturnFailureResult()
    {
        var error = Error.Failure(VectraErrors.SystemFailure, "Failure");

        var result = await PaginatedResult<string>.FailureAsync(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Theory]
    [InlineData(1, 10, 25, 3)]
    [InlineData(1, 10, 10, 1)]
    [InlineData(1, 10, 0, 0)]
    [InlineData(2, 5, 11, 3)]
    public void TotalPages_ShouldBeCorrectlyCeiled(int pageNumber, int pageSize, int totalCount, int expectedTotalPages)
    {
        var result = PaginatedResult<string>.Success([], pageNumber, pageSize, totalCount);

        result.TotalPages.Should().Be(expectedTotalPages);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void HasPreviousPage_ShouldBeTrueWhenPageNumberGreaterThanOne(int pageNumber, bool expected)
    {
        var result = PaginatedResult<string>.Success([], pageNumber, pageSize: 5, totalCount: 20);

        result.HasPreviousPage.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 5, 20, true)]
    [InlineData(4, 5, 20, false)]
    [InlineData(5, 5, 20, false)]
    public void HasNextPage_ShouldBeTrueWhenPageNumberLessThanTotalPages(int pageNumber, int pageSize, int totalCount, bool expected)
    {
        var result = PaginatedResult<string>.Success([], pageNumber, pageSize, totalCount);

        result.HasNextPage.Should().Be(expected);
    }
}
