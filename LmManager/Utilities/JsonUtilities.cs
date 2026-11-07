using System.Text.Json;

namespace LmManager.Utilities;

public static class JsonUtilities
{
    public static string? GetModelId(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var model in data.EnumerateArray())
        {
            if (model.ValueKind != JsonValueKind.Object ||
                !model.TryGetProperty("id", out var id) ||
                id.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(id.GetString()))
            {
                continue;
            }

            return id.GetString();
        }

        return null;
    }

    public static int? TryGetInteger(JsonElement element, IReadOnlyList<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetProperty(element, propertyName, out var value) &&
                value.TryGetInt32(out var result) &&
                result > 0)
            {
                return result;
            }
        }

        return null;
    }

    public static double? TryGetNumber(JsonElement element, IReadOnlyList<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetProperty(element, propertyName, out var value) &&
                value.TryGetDouble(out var result) &&
                double.IsFinite(result) &&
                result >= 0)
            {
                return result;
            }
        }

        return null;
    }

    public static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
