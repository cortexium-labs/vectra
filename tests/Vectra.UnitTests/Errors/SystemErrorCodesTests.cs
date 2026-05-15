using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;
using Vectra.Errors;

namespace Vectra.UnitTests.Errors;

public class SystemErrorCodesTests
{
    [Fact]
    public void AuthenticationRequired_HasCorrectValue()
    {
        SystemErrorCodes.AuthenticationRequired.Value.Should().Be(900_002);
    }

    [Fact]
    public void AuthenticationRequired_HasSystemCategory()
    {
        SystemErrorCodes.AuthenticationRequired.Category.Should().Be(ErrorCategory.System);
    }

    [Fact]
    public void AuthenticationRequired_ToStringIncludesPrefix()
    {
        SystemErrorCodes.AuthenticationRequired.ToString().Should().StartWith(ErrorCode.Prefix);
    }
}
