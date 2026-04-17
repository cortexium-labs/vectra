using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Vectra.Infrastructure.Decision;

public static class JsonToIntentText
{
    public static string Convert(string json)
    {
        var options = new JsonDocumentOptions
        {
            AllowTrailingCommas = true
        };

        using var doc = JsonDocument.Parse(json, options);
        var sb = new StringBuilder();
        ProcessElement(doc.RootElement, sb, prefix: null);
        return sb.ToString().Trim();
    }

    private static void ProcessElement(JsonElement element, StringBuilder sb, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var key = NormalizeKey(prop.Name);

                    if (IsNoise(key)) continue;

                    var newPrefix = string.IsNullOrEmpty(prefix)
                        ? key
                        : $"{prefix} {key}";

                    ProcessElement(prop.Value, sb, newPrefix);
                }
                break;

            case JsonValueKind.Array:
                var items = new List<string>();

                foreach (var item in element.EnumerateArray())
                {
                    var itemText = ExtractSimpleValue(item);
                    if (!string.IsNullOrEmpty(itemText))
                        items.Add(itemText);
                }

                if (items.Count > 0 && prefix != null)
                {
                    sb.AppendLine($"{prefix} {string.Join(", ", items)}");
                }
                else
                {
                    // fallback: process deeply
                    foreach (var item in element.EnumerateArray())
                    {
                        ProcessElement(item, sb, prefix);
                    }
                }
                break;

            default:
                var value = ExtractSimpleValue(element);
                if (!string.IsNullOrEmpty(value) && prefix != null)
                {
                    sb.AppendLine($"{prefix} {value}");
                }
                break;
        }
    }

    private static string ExtractSimpleValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => NormalizeValue(element.GetString()),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return key;

        // camelCase → camel case
        key = Regex.Replace(key, "([a-z])([A-Z])", "$1 $2", RegexOptions.None, TimeSpan.FromSeconds(3));

        // snake_case → snake case
        key = key.Replace("_", " ");

        return key.ToLowerInvariant();
    }

    private static string NormalizeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        return value.ToLowerInvariant();
    }

    private static bool IsNoise(string key)
    {
        // filter out common useless fields
        var noise = new HashSet<string>
        {
            "id", "request id", "trace id", "timestamp", "created at", "updated at"
        };

        return noise.Contains(key);
    }
}