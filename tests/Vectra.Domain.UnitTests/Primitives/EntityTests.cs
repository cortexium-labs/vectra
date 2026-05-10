using FluentAssertions;
using Vectra.Domain.AuditTrails;
using Vectra.Domain.Agents;
using Vectra.Domain.Primitives;

namespace Vectra.Domain.UnitTests.Primitives;

// Concrete Entity<Guid> subclasses used only in these tests
file sealed class EntityA : Entity<Guid>
{
    public EntityA(Guid id) : base(id) { }
}

file sealed class EntityB : Entity<Guid>
{
    public EntityB(Guid id) : base(id) { }
}

public class EntityTests
{
    // --- Equals(object?) branches ---

    [Fact]
    public void Equals_ShouldReturnFalse_WhenObjIsNotAnEntity()
    {
        var entity = new EntityA(Guid.NewGuid());

        entity.Equals("not an entity").Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameReference()
    {
        var entity = new EntityA(Guid.NewGuid());

        entity.Equals((object)entity).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentConcreteType_SameId()
    {
        var id = Guid.NewGuid();
        var a = new EntityA(id);
        var b = new EntityB(id);

        a.Equals((object)b).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameConcreteTypeAndSameId()
    {
        var id = Guid.NewGuid();
        var a1 = new EntityA(id);
        var a2 = new EntityA(id);

        a1.Equals((object)a2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenSameConcreteTypeButDifferentId()
    {
        var a1 = new EntityA(Guid.NewGuid());
        var a2 = new EntityA(Guid.NewGuid());

        a1.Equals((object)a2).Should().BeFalse();
    }

    // --- IEquatable<Entity<TId>>.Equals ---

    [Fact]
    public void EqualsTyped_ShouldReturnFalse_WhenNull()
    {
        var entity = new EntityA(Guid.NewGuid());

        entity.Equals((Entity<Guid>?)null).Should().BeFalse();
    }

    [Fact]
    public void EqualsTyped_ShouldReturnTrue_WhenSameTypeAndId()
    {
        var id = Guid.NewGuid();
        var a1 = new EntityA(id);
        var a2 = new EntityA(id);

        a1.Equals((Entity<Guid>)a2).Should().BeTrue();
    }

    // --- GetHashCode ---

    [Fact]
    public void GetHashCode_ShouldBeConsistentForSameEntity()
    {
        var entity = new EntityA(Guid.NewGuid());

        entity.GetHashCode().Should().Be(entity.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ShouldBeEqualForEntitiesWithSameTypeAndId()
    {
        var id = Guid.NewGuid();
        var a1 = new EntityA(id);
        var a2 = new EntityA(id);

        a1.GetHashCode().Should().Be(a2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ShouldDifferForDifferentIds()
    {
        var a1 = new EntityA(Guid.NewGuid());
        var a2 = new EntityA(Guid.NewGuid());

        a1.GetHashCode().Should().NotBe(a2.GetHashCode());
    }

    // --- operator == ---

    [Fact]
    public void OperatorEqual_ShouldReturnTrue_WhenBothNull()
    {
        EntityA? left = null;
        EntityA? right = null;

        (left == right).Should().BeTrue();
    }

    [Fact]
    public void OperatorEqual_ShouldReturnFalse_WhenLeftIsNull()
    {
        EntityA? left = null;
        var right = new EntityA(Guid.NewGuid());

        (left == right).Should().BeFalse();
    }

    [Fact]
    public void OperatorEqual_ShouldReturnFalse_WhenRightIsNull()
    {
        var left = new EntityA(Guid.NewGuid());
        EntityA? right = null;

        (left == right).Should().BeFalse();
    }

    [Fact]
    public void OperatorEqual_ShouldReturnTrue_WhenSameTypeAndId()
    {
        var id = Guid.NewGuid();
        Entity<Guid> a1 = new EntityA(id);
        Entity<Guid> a2 = new EntityA(id);

        (a1 == a2).Should().BeTrue();
    }

    // --- operator != ---

    [Fact]
    public void OperatorNotEqual_ShouldReturnTrue_WhenDifferentIds()
    {
        Entity<Guid> a1 = new EntityA(Guid.NewGuid());
        Entity<Guid> a2 = new EntityA(Guid.NewGuid());

        (a1 != a2).Should().BeTrue();
    }

    [Fact]
    public void OperatorNotEqual_ShouldReturnFalse_WhenSameTypeAndId()
    {
        var id = Guid.NewGuid();
        Entity<Guid> a1 = new EntityA(id);
        Entity<Guid> a2 = new EntityA(id);

        (a1 != a2).Should().BeFalse();
    }
}

public class AuditableEntityTests
{
    [Fact]
    public void AuditableEntity_ShouldStoreAuditProperties()
    {
        var now = DateTime.UtcNow;
        var agent = new Agent("Test", "owner", "hash")
        {
            CreatedBy = "user1",
            CreatedOn = now,
            LastModifiedBy = "user2",
            LastModifiedOn = now.AddMinutes(5)
        };

        agent.CreatedBy.Should().Be("user1");
        agent.CreatedOn.Should().Be(now);
        agent.LastModifiedBy.Should().Be("user2");
        agent.LastModifiedOn.Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public void AuditableEntity_PropertiesShouldBeNullByDefault()
    {
        var agent = new Agent("Test", "owner", "hash");

        agent.CreatedBy.Should().BeNull();
        agent.CreatedOn.Should().BeNull();
        agent.LastModifiedBy.Should().BeNull();
        agent.LastModifiedOn.Should().BeNull();
    }
}
