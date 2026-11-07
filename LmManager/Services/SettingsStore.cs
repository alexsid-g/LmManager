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
            return settings is null ? defaults : CreateSettings(settings, defaults);
        }
        catch (Exception ex)
        {
            var backupPath = GetBackupPath();
            if (File.Exists(backupPath))
            {
                try
                {
                    var backupSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(backupPath));
                    if (backupSettings is not null)
                    {
                        Console.Error.WriteLine(
                            $"Could not read settings from '{SettingsPath}' ({ex.Message}). Falling back to backup '{backupPath}'.");
                        return CreateSettings(backupSettings, defaults);
                    }
                }
                catch (Exception backupEx)
                {
                    Console.Error.WriteLine(
                        $"Could not read settings backup '{backupPath}' ({backupEx.Message}). Using defaults.");
                }
            }

            Console.Error.WriteLine($"Could not read settings from '{SettingsPath}' ({ex.Message}). Using defaults.");
            return defaults;
        }
    }

    private static AppSettings CreateSettings(AppSettings? source, AppSettings defaults) => new()
    {
        ModelsDirectory = GetValueOrDefault(source?.ModelsDirectory, defaults.ModelsDirectory),
        LogsDirectory = GetValueOrDefault(source?.LogsDirectory, defaults.LogsDirectory),
        MlxHost = GetValueOrDefault(source?.MlxHost, defaults.MlxHost),
        MlxPort = GetPortOrDefault(source?.MlxPort, defaults.MlxPort),
        MlxExecutable = GetValueOrDefault(source?.MlxExecutable, defaults.MlxExecutable),
        MlxVlmHost = GetValueOrDefault(source?.MlxVlmHost, defaults.MlxVlmHost),
        MlxVlmPort = GetPortOrDefault(source?.MlxVlmPort, defaults.MlxVlmPort),
        MlxVlmExecutable = GetValueOrDefault(source?.MlxVlmExecutable, defaults.MlxVlmExecutable),
        LlamaHost = GetValueOrDefault(source?.LlamaHost, defaults.LlamaHost),
        LlamaPort = GetPortOrDefault(source?.LlamaPort, defaults.LlamaPort),
        LlamaExecutable = GetValueOrDefault(source?.LlamaExecutable, defaults.LlamaExecutable),
        ModelContextSizes = GetContextSizesOrDefault(source?.ModelContextSizes),
        ModelTemperatures = GetTemperaturesOrDefault(source?.ModelTemperatures),
        ModelReasonings = GetReasoningsOrDefault(source?.ModelReasonings)
    };

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

        if (File.Exists(SettingsPath))
            File.Copy(SettingsPath, GetBackupPath(), overwrite: true);

        var temporaryPath = $"{SettingsPath}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }

    private string GetBackupPath() => $"{SettingsPath}.bak";
}
