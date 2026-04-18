using FluentAssertions;
using Vectra.BuildingBlocks.Errors;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Errors;

public class ErrorCodeTests
{
    [Fact]
    public void ToString_ShouldFormatWithPrefixAndSixDigits()
    {
        var errorCode = new ErrorCode(1, ErrorCategory.System);

        errorCode.ToString().Should().Be("VEC000001");
    }

    [Fact]
    public void ToString_ShouldPadValueToSixDigits()
    {
        var errorCode = new ErrorCode(12345, ErrorCategory.Core);

        errorCode.ToString().Should().Be("VEC012345");
    }

    [Fact]
    public void Prefix_ShouldBeVEC()
    {
        ErrorCode.Prefix.Should().Be("VEC");
    }

    [Fact]
    public void RecordEquality_ShouldConsiderValueAndCategory()
    {
        var code1 = new ErrorCode(1000, ErrorCategory.Core);
        var code2 = new ErrorCode(1000, ErrorCategory.Core);
        var code3 = new ErrorCode(1000, ErrorCategory.Security);

        code1.Should().Be(code2);
        code1.Should().NotBe(code3);
    }

    [Fact]
    public void VectraErrors_SystemFailure_ShouldHaveCorrectCode()
    {
        VectraErrors.SystemFailure.Value.Should().Be(1);
        VectraErrors.SystemFailure.Category.Should().Be(ErrorCategory.System);
    }

    [Fact]
    public void VectraErrors_ValidationFailed_ShouldHaveCoreCategory()
    {
        VectraErrors.ValidationFailed.Category.Should().Be(ErrorCategory.Core);
    }
}
