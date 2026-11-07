using System.Text.Json;
using LmManager.Utilities;

namespace LmManager.Services;

public sealed class ParametersProvider
{
    public string[] LoadParameters(string filePath, string modelName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(filePath));
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !JsonUtilities.TryGetProperty(root, "name", out var name) ||
            name.ValueKind != JsonValueKind.String ||
            !JsonUtilities.TryGetProperty(root, "params", out var parameters) ||
            parameters.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Expected an object with string 'name' and object 'params' properties.");
        }

        if (!string.Equals(name.GetString(), modelName, StringComparison.OrdinalIgnoreCase))
            return [];

        var arguments = new List<string>();
        foreach (var parameter in parameters.EnumerateObject())
        {
            if (string.IsNullOrWhiteSpace(parameter.Name) ||
                parameter.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Object or JsonValueKind.Array)
            {
                throw new JsonException($"Invalid value for startup parameter '{parameter.Name}'.");
            }

            arguments.Add(parameter.Name.StartsWith("-", StringComparison.Ordinal)
                ? parameter.Name
                : $"--{parameter.Name}");
            arguments.Add(parameter.Value.ValueKind == JsonValueKind.String
                ? parameter.Value.GetString()!
                : parameter.Value.ToString());
        }

        return arguments.ToArray();
    }
}
