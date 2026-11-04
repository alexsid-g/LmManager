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
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
            var defaults = new AppSettings();
            if (string.IsNullOrWhiteSpace(settings.ModelsDirectory))
                settings.ModelsDirectory = defaults.ModelsDirectory;
            if (string.IsNullOrWhiteSpace(settings.LogsDirectory))
                settings.LogsDirectory = defaults.LogsDirectory;
            if (string.IsNullOrWhiteSpace(settings.MlxHost))
                settings.MlxHost = defaults.MlxHost;
            if (settings.MlxPort is < 1 or > 65535)
                settings.MlxPort = defaults.MlxPort;
            if (string.IsNullOrWhiteSpace(settings.MlxExecutable))
                settings.MlxExecutable = defaults.MlxExecutable;
            if (string.IsNullOrWhiteSpace(settings.MlxVlmHost))
                settings.MlxVlmHost = defaults.MlxVlmHost;
            if (settings.MlxVlmPort is < 1 or > 65535)
                settings.MlxVlmPort = defaults.MlxVlmPort;
            if (string.IsNullOrWhiteSpace(settings.MlxVlmExecutable))
                settings.MlxVlmExecutable = defaults.MlxVlmExecutable;
            if (string.IsNullOrWhiteSpace(settings.LlamaHost))
                settings.LlamaHost = defaults.LlamaHost;
            if (settings.LlamaPort is < 1 or > 65535)
                settings.LlamaPort = defaults.LlamaPort;
            if (string.IsNullOrWhiteSpace(settings.LlamaExecutable))
                settings.LlamaExecutable = defaults.LlamaExecutable;
            return settings;
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{SettingsPath}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }
}
