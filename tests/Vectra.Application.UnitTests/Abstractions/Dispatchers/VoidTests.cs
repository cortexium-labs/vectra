using FluentAssertions;
using VoidType = Vectra.Application.Abstractions.Dispatchers.Void;

namespace Vectra.Application.UnitTests.Abstractions.Dispatchers;

public class VoidTests
{
    [Fact]
    public void Equals_TwoVoidInstances_ShouldBeEqual()
    {
        var a = new VoidType();
        var b = new VoidType();

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_BoxedVoidObject_ShouldBeTrue()
    {
        var v = new VoidType();
        v.Equals((object)new VoidType()).Should().BeTrue();
    }

    [Fact]
    public void Equals_NonVoidObject_ShouldBeFalse()
    {
        var v = new VoidType();
        v.Equals("not void").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldReturnZero()
    {
        new VoidType().GetHashCode().Should().Be(0);
    }

    [Fact]
    public void StaticValue_ShouldBeDefaultVoid()
    {
        VoidType.Value.Should().BeOfType<VoidType>();
        VoidType.Value.Equals(new VoidType()).Should().BeTrue();
    }
}
