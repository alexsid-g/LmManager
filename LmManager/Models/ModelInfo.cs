namespace LmManager.Models;

public enum ModelFormat
{
    Mlx,
    Gguf
}

public sealed record ModelInfo(
    string Name,
    string Path,
    ModelFormat Format,
    long SizeBytes,
    string? MmprojFileName = null)
{
    public const int DefaultContextSize = 131072;
    public const double DefaultTemperature = 0.7;

    public static IReadOnlyList<(double Value, string Label)> TemperatureOptions { get; } =
        Array.AsReadOnly<(double Value, string Label)>(
        [
            (0.3, "Development (0.3)"),
            (0.5, "Analytics (0.5)"),
            (0.7, "Regular (0.7)"),
            (0.9, "Creative (0.9)"),
        ]);

    public IReadOnlyList<int> ContextSizeOptions { get; } =  [32768, 65536, 131072];

    public int ContextSize { get; init; } = DefaultContextSize;
    public double Temperature { get; init; } = DefaultTemperature;
    public bool HasMmproj => !string.IsNullOrWhiteSpace(MmprojFileName);

    public string DisplayName => 
        $"{Name}{(HasMmproj ? " (+mmproj)" : string.Empty)} ({Format}, {FormatSize(SizeBytes)})";

    public static bool IsSupportedTemperature(double temperature) =>
        TemperatureOptions.Any(option => option.Value == temperature);

    private static string FormatSize(long sizeBytes) =>
        sizeBytes >= 1024L * 1024 * 1024
            ? $"{sizeBytes / (1024d * 1024 * 1024):0.0} GB"
            : $"{sizeBytes / (1024d * 1024):0} MB";
}

public sealed record ServerStatus(bool IsRunning, string? CurrentModel = null);
