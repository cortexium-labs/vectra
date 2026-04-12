using System.Text.Json;
using System.Text.RegularExpressions;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Policy;

public static class PolicyEvaluator
{
    public static bool EvaluateCondition(
        PolicyRuleCondition condition, 
        Dictionary<string, object> input)
    {
        var attributeValue = GetValueFromPath(condition.Field, input);
        var expectedValue = DeserializeValue(condition.Value);
        return condition.Operator switch
        {
            "eq" => Equals(attributeValue, expectedValue),
            "ne" => !Equals(attributeValue, expectedValue),
            "gt" => Compare(attributeValue, expectedValue) > 0,
            "lt" => Compare(attributeValue, expectedValue) < 0,
            "ge" => Compare(attributeValue, expectedValue) >= 0,
            "le" => Compare(attributeValue, expectedValue) <= 0,
            "in" => IsIn(attributeValue, expectedValue),
            "contains" => Contains(attributeValue, expectedValue),
            "startsWith" => StartsWith(attributeValue, expectedValue),
            "endsWith" => EndsWith(attributeValue, expectedValue),
            "regex" => RegexMatch(attributeValue, expectedValue),
            _ => false
        };
    }

    private static object? GetValueFromPath(string path, Dictionary<string, object> input)
    {
        if (path.StartsWith("input."))
            return GetNestedValue(path.Substring(6), input);
        return GetNestedValue(path, input); // fallback to input
    }

    private static object? GetNestedValue(string path, Dictionary<string, object> root)
    {
        var parts = path.Split('.');
        object? current = root;
        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict && dict.TryGetValue(part, out var val))
                current = val;
            else if (current is JsonElement json && json.TryGetProperty(part, out var prop))
                current = ConvertJsonElement(prop);
            else
                return null;
        }
        return current;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            _ => null
        };
    }

    private static object? DeserializeValue(object? value)
    {
        if (value is null) return null;

        return value switch
        {
            JsonElement je => ConvertJsonElement(je), // already JSON token
            string s when string.IsNullOrWhiteSpace(s) => null,
            _ => value // keep plain scalar/object as-is
        };
    }

    private static int Compare(object? a, object? b)
    {
        if (a is IComparable ca && b is IComparable cb)
            return ca.CompareTo(cb);
        return 0;
    }

    private static bool IsIn(object? value, object? collection)
    {
        if (collection is IEnumerable<object> list)
            return list.Any(item => Equals(item, value));
        if (collection is JsonElement json && json.ValueKind == JsonValueKind.Array)
            return json.EnumerateArray().Any(e => Equals(ConvertJsonElement(e), value));
        return false;
    }

    private static bool Contains(object? container, object? item)
    {
        if (container is string str && item is string sub)
            return str.Contains(sub, StringComparison.OrdinalIgnoreCase);
        if (container is IEnumerable<object> list)
            return list.Any(x => Equals(x, item));
        return false;
    }

    private static bool StartsWith(object? str, object? prefix)
    {
        return str is string s && prefix is string p && s.StartsWith(p, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EndsWith(object? str, object? suffix)
    {
        return str is string s && suffix is string p && s.EndsWith(p, StringComparison.OrdinalIgnoreCase);
    }

    private static bool RegexMatch(object? str, object? pattern)
    {
        return str is string s && pattern is string p && Regex.IsMatch(s, p);
    }
}