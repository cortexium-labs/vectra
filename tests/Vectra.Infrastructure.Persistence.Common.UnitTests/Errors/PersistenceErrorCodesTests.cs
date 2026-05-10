using FluentAssertions;
using Vectra.BuildingBlocks.Errors;
using Vectra.Infrastructure.Persistence.Common.Errors;

namespace Vectra.Infrastructure.Persistence.Common.UnitTests.Errors;

public class PersistenceErrorCodesTests
{
    [Fact]
    public void DatabaseSaveData_ShouldHaveCorrectValue()
    {
        PersistenceErrorCodes.DatabaseSaveData.Value.Should().Be(300_001);
        PersistenceErrorCodes.DatabaseSaveData.Category.Should().Be(ErrorCategory.Persistence);
    }

    [Fact]
    public void DatabaseModelCreating_ShouldHaveCorrectValue()
    {
        PersistenceErrorCodes.DatabaseModelCreating.Value.Should().Be(300_002);
        PersistenceErrorCodes.DatabaseModelCreating.Category.Should().Be(ErrorCategory.Persistence);
    }

    [Fact]
    public void DatabaseInitializer_ShouldHaveCorrectValue()
    {
        PersistenceErrorCodes.DatabaseInitializer.Value.Should().Be(300_003);
        PersistenceErrorCodes.DatabaseInitializer.Category.Should().Be(ErrorCategory.Persistence);
    }

    [Fact]
    public void AllErrorCodes_ShouldBeUnique()
    {
        var codes = new[]
        {
            PersistenceErrorCodes.DatabaseSaveData,
            PersistenceErrorCodes.DatabaseModelCreating,
            PersistenceErrorCodes.DatabaseInitializer
        };

        codes.Should().OnlyHaveUniqueItems();
    }
}
