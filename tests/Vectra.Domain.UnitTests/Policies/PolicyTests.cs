using FluentAssertions;
using Vectra.Domain.Policies;

namespace Vectra.Domain.UnitTests.Policies;

public class PolicyDefinitionTests
{
    [Fact]
    public void PolicyDefinition_ShouldHaveDefaultDenyEffect()
    {
        var policy = new PolicyDefinition();

        policy.Default.Should().Be(PolicyType.Deny);
    }

    [Fact]
    public void PolicyDefinition_ShouldHaveEmptyRulesByDefault()
    {
        var policy = new PolicyDefinition();

        policy.Rules.Should().BeEmpty();
    }

    [Fact]
    public void PolicyDefinition_ShouldStoreNameAndOwner()
    {
        var policy = new PolicyDefinition
        {
            Name = "FinancePolicy",
            Owner = "admin",
            Description = "Finance department policy"
        };

        policy.Name.Should().Be("FinancePolicy");
        policy.Owner.Should().Be("admin");
        policy.Description.Should().Be("Finance department policy");
    }

    [Fact]
    public void PolicyDefinition_ShouldStoreCreatedOn()
    {
        var now = DateTime.UtcNow;
        var policy = new PolicyDefinition { CreatedOn = now };

        policy.CreatedOn.Should().Be(now);
    }

    [Fact]
    public void PolicyDefinition_CreatedOn_ShouldBeNullByDefault()
    {
        var policy = new PolicyDefinition();

        policy.CreatedOn.Should().BeNull();
    }

    [Fact]
    public void PolicyDefinition_ShouldAllowAllowAsDefaultEffect()
    {
        var policy = new PolicyDefinition { Default = PolicyType.Allow };

        policy.Default.Should().Be(PolicyType.Allow);
    }
}

public class PolicyRuleTests
{
    [Fact]
    public void PolicyRule_ShouldHaveDefaultAllowEffectAndZeroPriority()
    {
        var rule = new PolicyRule();

        rule.Effect.Should().Be(PolicyType.Allow);
        rule.Priority.Should().Be(0);
        rule.Conditions.Should().BeEmpty();
    }

    [Fact]
    public void PolicyRule_ShouldStoreNameReasonAndPriority()
    {
        var rule = new PolicyRule
        {
            Name = "BlockDelete",
            Reason = "Deletes are not permitted",
            Priority = 10,
            Effect = PolicyType.Deny
        };

        rule.Name.Should().Be("BlockDelete");
        rule.Reason.Should().Be("Deletes are not permitted");
        rule.Priority.Should().Be(10);
        rule.Effect.Should().Be(PolicyType.Deny);
    }

    [Fact]
    public void PolicyRule_ShouldStoreConditions()
    {
        var rule = new PolicyRule
        {
            Conditions =
            [
                new PolicyRuleCondition { Field = "method", Operator = "eq", Value = "DELETE" },
                new PolicyRuleCondition { Field = "path", Operator = "startsWith", Value = "/admin" }
            ]
        };

        rule.Conditions.Should().HaveCount(2);
        rule.Conditions.Should().Contain(c => c.Field == "method" && c.Operator == "eq");
        rule.Conditions.Should().Contain(c => c.Field == "path" && c.Operator == "startsWith");
    }
}

public class PolicyRuleConditionTests
{
    [Fact]
    public void PolicyRuleCondition_ShouldStoreFieldOperatorAndValue()
    {
        var condition = new PolicyRuleCondition
        {
            Field = "user.role",
            Operator = "in",
            Value = new[] { "admin", "superuser" }
        };

        condition.Field.Should().Be("user.role");
        condition.Operator.Should().Be("in");
        condition.Value.Should().BeEquivalentTo(new[] { "admin", "superuser" });
    }
}
