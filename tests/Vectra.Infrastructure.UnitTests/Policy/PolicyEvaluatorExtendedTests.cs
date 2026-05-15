using FluentAssertions;
using System.Text.Json;
using Vectra.Domain.Policies;
using Vectra.Infrastructure.Policy;

namespace Vectra.Infrastructure.UnitTests.Policy;

/// <summary>
/// Tests that cover uncovered branches: ConvertJsonElement (all kinds),
/// IsIn with JsonElement array, Contains with IEnumerable, Compare with non-comparable,
/// GetNestedValue with JsonElement traversal.
/// </summary>
public class PolicyEvaluatorExtendedTests
{
    private static Dictionary<string, object> Input(params (string key, object value)[] pairs)
        => pairs.ToDictionary(p => p.key, p => p.value);

    // ── ConvertJsonElement via nested traversal ───────────────────────────
    // The ConvertJsonElement branches are exercised when a JsonElement Object
    // is stored in the dict and a nested property is accessed via GetNestedValue.

    [Fact]
    public void EvaluateCondition_Eq_NestedJsonElementString_ReturnsTrue()
    {
        // Exercises ConvertJsonElement → String branch
        var json = JsonSerializer.SerializeToElement(new { nested = new { method = "DELETE" } });
        var condition = new PolicyRuleCondition { Field = "input.data.nested.method", Operator = "eq", Value = "DELETE" };
        var input = new Dictionary<string, object> { ["data"] = json };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Eq_NestedJsonElementNumberInt_ReturnsTrue()
    {
        // Exercises ConvertJsonElement → Number int branch; JSON ints with TryGetInt32
        // are stored as int, value must match the same type for Equals
        var json = JsonSerializer.SerializeToElement(new { count = 42 });
        var condition = new PolicyRuleCondition { Field = "input.data.count", Operator = "ge", Value = 41.0 };
        var input = new Dictionary<string, object> { ["data"] = json };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Eq_NestedJsonElementDouble_WorksWithGt()
    {
        // Exercises ConvertJsonElement → Number double branch
        var json = JsonSerializer.SerializeToElement(new { score = 3.14 });
        var condition = new PolicyRuleCondition { Field = "input.data.score", Operator = "gt", Value = 3.0 };
        var input = new Dictionary<string, object> { ["data"] = json };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Eq_NestedJsonElementBoolTrue_ReturnsTrue()
    {
        // Exercises ConvertJsonElement → True branch
        var json = JsonSerializer.SerializeToElement(new { flag = true });
        var condition = new PolicyRuleCondition { Field = "input.data.flag", Operator = "eq", Value = true };
        var input = new Dictionary<string, object> { ["data"] = json };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Eq_NestedJsonElementBoolFalse_ReturnsTrue()
    {
        // Exercises ConvertJsonElement → False branch
        var json = JsonSerializer.SerializeToElement(new { flag = false });
        var condition = new PolicyRuleCondition { Field = "input.data.flag", Operator = "eq", Value = false };
        var input = new Dictionary<string, object> { ["data"] = json };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Eq_NestedJsonElementNull_ReturnsFalse()
    {
        // Exercises ConvertJsonElement → Null branch (returns null)
        var json = JsonDocument.Parse("""{"val":null}""").RootElement;
        var condition = new PolicyRuleCondition { Field = "input.data.val", Operator = "eq", Value = "something" };
        var input = new Dictionary<string, object> { ["data"] = json };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_Eq_NestedJsonElementObject_ReturnsDictionary()
    {
        // Exercises ConvertJsonElement → Object branch (returns Dictionary)
        var json = JsonSerializer.SerializeToElement(new { nested = new { x = 1 } });
        var condition = new PolicyRuleCondition { Field = "input.data.nested", Operator = "eq", Value = null! };
        var input = new Dictionary<string, object> { ["data"] = json };

        // nested is an object (dict), not null
        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_Contains_NestedJsonElementArray_ValueFound_ReturnsTrue()
    {
        // Exercises ConvertJsonElement → Array branch (returns List) via Contains
        var json = JsonSerializer.SerializeToElement(new { roles = new[] { "admin", "user" } });
        var condition = new PolicyRuleCondition { Field = "input.data.roles", Operator = "contains", Value = "admin" };
        var input = new Dictionary<string, object> { ["data"] = json };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    // ── DeserializeValue with JsonElement condition value ─────────────────

    [Fact]
    public void EvaluateCondition_In_JsonElementArray_ValueFound_ReturnsTrue()
    {
        // DeserializeValue converts JsonElement array to list for IsIn check
        var jsonArray = JsonSerializer.SerializeToElement(new[] { "GET", "POST", "PUT" });
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "in", Value = jsonArray };
        var input = Input(("method", "GET"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_In_JsonElementArray_ValueNotFound_ReturnsFalse()
    {
        var jsonArray = JsonSerializer.SerializeToElement(new[] { "GET", "POST" });
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "in", Value = jsonArray };
        var input = Input(("method", "DELETE"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_Contains_List_ValueFound_ReturnsTrue()
    {
        var list = new List<object> { "admin", "superuser" };
        var condition = new PolicyRuleCondition { Field = "input.role", Operator = "contains", Value = "admin" };
        var input = new Dictionary<string, object> { ["role"] = list };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Contains_List_ValueNotFound_ReturnsFalse()
    {
        var list = new List<object> { "read", "write" };
        var condition = new PolicyRuleCondition { Field = "input.perms", Operator = "contains", Value = "delete" };
        var input = new Dictionary<string, object> { ["perms"] = list };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_Gt_NonComparable_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.obj", Operator = "gt", Value = new object() };
        var input = new Dictionary<string, object> { ["obj"] = new object() };

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_DeserializeValue_NullValue_ReturnsFalseOnEq()
    {
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "eq", Value = null! };
        var input = Input(("method", "GET"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_DeserializeValue_WhitespaceString_TreatedAsNull()
    {
        var condition = new PolicyRuleCondition { Field = "input.method", Operator = "eq", Value = "   " };
        var input = Input(("method", "GET"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_StartsWith_NonStringValue_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.count", Operator = "startswith", Value = "5" };
        var input = Input(("count", 5));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_EndsWith_NonStringValue_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.count", Operator = "endswith", Value = "5" };
        var input = Input(("count", 5));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_Regex_NonStringValue_ReturnsFalse()
    {
        var condition = new PolicyRuleCondition { Field = "input.count", Operator = "regex", Value = @"\d+" };
        var input = Input(("count", 5));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_StartsWith_CaseInsensitive_Uppercase_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "input.path", Operator = "startswith", Value = "/API" };
        var input = Input(("path", "/api/data"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_FieldWithoutInputPrefix_ValueInRoot_ReturnsTrue()
    {
        var condition = new PolicyRuleCondition { Field = "method", Operator = "eq", Value = "GET" };
        var input = Input(("method", "GET"));

        PolicyEvaluator.EvaluateCondition(condition, input).Should().BeTrue();
    }
}
