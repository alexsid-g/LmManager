using System.Text.Json;
using LmManager.Models;

namespace LmManager.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LmManager",
        "settings.json");

    public AppSettings Load()
    {
        var defaults = new AppSettings();
        if (!File.Exists(SettingsPath))
            return defaults;

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
            return new AppSettings
            {
                ModelsDirectory = GetValueOrDefault(settings?.ModelsDirectory, defaults.ModelsDirectory),
                LogsDirectory = GetValueOrDefault(settings?.LogsDirectory, defaults.LogsDirectory),
                MlxHost = GetValueOrDefault(settings?.MlxHost, defaults.MlxHost),
                MlxPort = GetPortOrDefault(settings?.MlxPort, defaults.MlxPort),
                MlxExecutable = GetValueOrDefault(settings?.MlxExecutable, defaults.MlxExecutable),
                MlxVlmHost = GetValueOrDefault(settings?.MlxVlmHost, defaults.MlxVlmHost),
                MlxVlmPort = GetPortOrDefault(settings?.MlxVlmPort, defaults.MlxVlmPort),
                MlxVlmExecutable = GetValueOrDefault(settings?.MlxVlmExecutable, defaults.MlxVlmExecutable),
                LlamaHost = GetValueOrDefault(settings?.LlamaHost, defaults.LlamaHost),
                LlamaPort = GetPortOrDefault(settings?.LlamaPort, defaults.LlamaPort),
                LlamaExecutable = GetValueOrDefault(settings?.LlamaExecutable, defaults.LlamaExecutable),
                ModelContextSizes = GetContextSizesOrDefault(settings?.ModelContextSizes),
                ModelTemperatures = GetTemperaturesOrDefault(settings?.ModelTemperatures),
                ModelReasonings = GetReasoningsOrDefault(settings?.ModelReasonings)
            };
        }
        catch (Exception)
        {
            return defaults;
        }
    }

    private static string GetValueOrDefault(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value;

    private static int GetPortOrDefault(int? port, int defaultValue) =>
        port is >= 1 and <= 65535 ? port.Value : defaultValue;

    private static Dictionary<string, int> GetContextSizesOrDefault(Dictionary<string, int>? contextSizes) =>
        contextSizes is null
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : contextSizes
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Value > 0)
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

    private static Dictionary<string, double> GetTemperaturesOrDefault(Dictionary<string, double>? temperatures) =>
        temperatures is null
            ? new Dictionary<string, double>(StringComparer.Ordinal)
            : temperatures.Where(
                entry => !string.IsNullOrWhiteSpace(entry.Key) &&
                ModelInfo.IsSupportedTemperature(entry.Value))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

    private static Dictionary<string, ReasoningProfile> GetReasoningsOrDefault(
        Dictionary<string, ReasoningProfile>? reasonings) =>
        reasonings is null
            ? new Dictionary<string, ReasoningProfile>(StringComparer.Ordinal)
            : reasonings
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) &&
                    ModelInfo.IsSupportedReasoning(entry.Value))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{SettingsPath}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }
}
