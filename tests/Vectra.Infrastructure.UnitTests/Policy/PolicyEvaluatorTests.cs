using FluentAssertions;
using System.Text.Json;
using Vectra.Domain.Policies;
using Vectra.Infrastructure.Policy;

namespace Vectra.Infrastructure.UnitTests.Policy;

public class PolicyEvaluatorTests
{
    private static Dictionary<string, object> Input(params (string key, object value)[] pairs)
        => pairs.ToDictionary(p => p.key, p => p.value);

    // --- eq operator ---
    [Fact]
    public void EvaluateCondition_Eq_MatchingValue_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "eq", Value = "DELETE" };
        var input = Input(("method", "DELETE"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Eq_NonMatchingValue_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "eq", Value = "GET" };
        var input = Input(("method", "DELETE"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    // --- ne operator ---
    [Fact]
    public void EvaluateCondition_Ne_DifferentValue_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "ne", Value = "GET" };
        var input = Input(("method", "DELETE"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Ne_SameValue_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "ne", Value = "DELETE" };
        var input = Input(("method", "DELETE"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    // --- gt / lt / ge / le operators ---
    [Fact]
    public void EvaluateCondition_Gt_GreaterValue_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.count", Operator = "gt", Value = 5 };
        var input = Input(("count", 10));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Lt_LesserValue_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.count", Operator = "lt", Value = 10 };
        var input = Input(("count", 5));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Ge_EqualValue_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.count", Operator = "ge", Value = 5 };
        var input = Input(("count", 5));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Le_EqualValue_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.count", Operator = "le", Value = 5 };
        var input = Input(("count", 5));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    // --- contains operator ---
    [Fact]
    public void EvaluateCondition_Contains_SubstringMatch_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.path", Operator = "contains", Value = "/admin" };
        var input = Input(("path", "/admin/users"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Contains_NoMatch_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.path", Operator = "contains", Value = "/secret" };
        var input = Input(("path", "/api/users"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    // --- startswith operator ---
    [Fact]
    public void EvaluateCondition_StartsWith_Match_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.path", Operator = "startswith", Value = "/api" };
        var input = Input(("path", "/api/data"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_StartsWith_NoMatch_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.path", Operator = "startswith", Value = "/admin" };
        var input = Input(("path", "/api/data"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    // --- endswith operator ---
    [Fact]
    public void EvaluateCondition_EndsWith_Match_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.path", Operator = "endswith", Value = "/export" };
        var input = Input(("path", "/api/data/export"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_EndsWith_NoMatch_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.path", Operator = "endswith", Value = "/import" };
        var input = Input(("path", "/api/data/export"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    // --- regex operator ---
    [Fact]
    public void EvaluateCondition_Regex_Match_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.path", Operator = "regex", Value = @"^/api/v\d+/" };
        var input = Input(("path", "/api/v2/users"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Regex_NoMatch_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.path", Operator = "regex", Value = @"^/admin/" };
        var input = Input(("path", "/api/data"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    // --- in operator ---
    [Fact]
    public void EvaluateCondition_In_ValueInList_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "in", Value = new List<object> { "POST", "PUT" } };
        var input = Input(("method", "POST"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_In_ValueNotInList_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "in", Value = new List<object> { "POST", "PUT" } };
        var input = Input(("method", "GET"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    // --- unknown operator ---
    [Fact]
    public void EvaluateCondition_UnknownOperator_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "unknown_op", Value = "GET" };
        var input = Input(("method", "GET"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    // --- missing field ---
    [Fact]
    public void EvaluateCondition_MissingField_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.missing", Operator = "eq", Value = "value" };
        var input = Input(("method", "GET"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    // --- nested field path ---
    [Fact]
    public void EvaluateCondition_NestedField_ResolvesCorrectly()
    {
        var condition = new PolicyRuleCondition { Field = "input.user.role", Operator = "eq", Value = "admin" };
        var input = new Dictionary<string, object>
        {
            ["user"] = new Dictionary<string, object> { ["role"] = "admin" }
        };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    // --- field without "input." prefix ---
    [Fact]
    public void EvaluateCondition_FieldWithoutInputPrefix_FallsBackToRoot()
    {
        var condition = new PolicyRuleCondition { Field = "method", Operator = "eq", Value = "GET" };
        var input = Input(("method", "GET"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    // --- case-insensitive string comparison ---
    [Fact]
    public void EvaluateCondition_StartsWith_CaseInsensitive_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.path", Operator = "startswith", Value = "/API" };
        var input = Input(("path", "/api/data"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }
}
