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
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
            var defaults = new AppSettings();
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
                LlamaExecutable = GetValueOrDefault(settings?.LlamaExecutable, defaults.LlamaExecutable)
            };
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    private static string GetValueOrDefault(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value;

    private static int GetPortOrDefault(int? port, int defaultValue) =>
        port is >= 1 and <= 65535 ? port.Value : defaultValue;

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{SettingsPath}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }
}
