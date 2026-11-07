using System.Collections.ObjectModel;
using System.Text.Json;
using LmManager.Models;
using LmManager.Utilities;

namespace LmManager.Services;

public sealed class ParametersProvider
{
    public const string FileName = "startup-params.json";
    private static readonly ReadOnlyDictionary<string, string> EmptyParams = new(
        new Dictionary<string, string>());

    public static bool HasParamsFile(string directory) =>
        File.Exists(Path.Combine(directory, FileName));

    public IDictionary<string, string> LoadParameters(ModelInfo model)
    {
        var modelDirectory = model.Format switch
        {
            ModelFormat.Mlx => model.Path,
            ModelFormat.Gguf => Path.GetDirectoryName(model.Path),
            _ => throw new ArgumentOutOfRangeException(nameof(model), model.Format, null)
        };
        if (string.IsNullOrWhiteSpace(modelDirectory))
            throw new InvalidOperationException($"Could not determine the directory for model '{model.Name}'.");

        var filePath = Path.Combine(modelDirectory, FileName);
        return HasParamsFile(modelDirectory)
            ? LoadParameters(filePath, model.Name)
            : EmptyParams;
    }

    private static IDictionary<string, string> LoadParameters(string filePath, string modelName)
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
            return EmptyParams;

        var arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters.EnumerateObject())
        {
            if (string.IsNullOrWhiteSpace(parameter.Name) ||
                parameter.Value.ValueKind is JsonValueKind.Null 
                or JsonValueKind.Object 
                or JsonValueKind.Array)
            {
                throw new JsonException($"Invalid value for startup parameter '{parameter.Name}'.");
            }

            // be simple - no checks
            var paramName = $"--{parameter.Name}";
            if (!arguments.ContainsKey(paramName))
            {
                var paramValue = parameter.Value.ValueKind switch
                {
                    JsonValueKind.String => parameter.Value.GetString()!,
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => parameter.Value.ToString()
                };

                arguments.Add(paramName, paramValue);
            }
        }

        return arguments;
    }
}
