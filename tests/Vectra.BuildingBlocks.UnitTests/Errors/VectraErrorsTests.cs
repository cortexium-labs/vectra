using FluentAssertions;
using Vectra.BuildingBlocks.Errors;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Errors;

public class VectraErrorsTests
{
    [Fact]
    public void SystemFailure_ShouldHaveSystemCategory()
    {
        VectraErrors.SystemFailure.Category.Should().Be(ErrorCategory.System);
        VectraErrors.SystemFailure.Value.Should().Be(1);
    }

    [Fact]
    public void FileNotFound_ShouldHaveInfrastructureCategory()
    {
        VectraErrors.FileNotFound.Category.Should().Be(ErrorCategory.Infrastructure);
    }

    [Fact]
    public void SerializationErrors_ShouldHaveSerializationCategory()
    {
        VectraErrors.SerializationFailed.Category.Should().Be(ErrorCategory.Serialization);
        VectraErrors.DeserializationFailed.Category.Should().Be(ErrorCategory.Serialization);
        VectraErrors.InvalidJson.Category.Should().Be(ErrorCategory.Serialization);
    }

    [Fact]
    public void Unauthorized_ShouldHaveSecurityCategory()
    {
        VectraErrors.Unauthorized.Category.Should().Be(ErrorCategory.Security);
    }

    [Fact]
    public void ValidationErrors_ShouldHaveCoreCategory()
    {
        VectraErrors.ValidationFailed.Category.Should().Be(ErrorCategory.Core);
        VectraErrors.RequiredFieldMissing.Category.Should().Be(ErrorCategory.Core);
    }

    [Fact]
    public void ResourceNotFound_ShouldHavePersistenceCategory()
    {
        VectraErrors.ResourceNotFound.Category.Should().Be(ErrorCategory.Persistence);
    }

    [Fact]
    public void DuplicateResource_ShouldHavePersistenceCategory()
    {
        VectraErrors.DuplicateResource.Category.Should().Be(ErrorCategory.Persistence);
    }

    [Fact]
    public void SecurityErrors_ShouldHaveSecurityCategory()
    {
        VectraErrors.AccessDenied.Category.Should().Be(ErrorCategory.Security);
        VectraErrors.MissingCredentials.Category.Should().Be(ErrorCategory.Security);
        VectraErrors.ExpiredSession.Category.Should().Be(ErrorCategory.Security);
    }

    [Fact]
    public void AllErrorCodes_ShouldHaveUniqueValues()
    {
        var codes = new[]
        {
            VectraErrors.SystemFailure.Value,
            VectraErrors.FileNotFound.Value,
            VectraErrors.SerializationFailed.Value,
            VectraErrors.DeserializationFailed.Value,
            VectraErrors.InvalidJson.Value,
            VectraErrors.Unauthorized.Value,
            VectraErrors.ValidationFailed.Value,
            VectraErrors.RequiredFieldMissing.Value,
            VectraErrors.ResourceNotFound.Value,
            VectraErrors.DuplicateResource.Value,
            VectraErrors.AccessDenied.Value,
            VectraErrors.MissingCredentials.Value,
            VectraErrors.ExpiredSession.Value
        };

        codes.Should().OnlyHaveUniqueItems();
    }
}
