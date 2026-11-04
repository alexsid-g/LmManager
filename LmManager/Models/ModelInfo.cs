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
    public bool HasMmproj => !string.IsNullOrWhiteSpace(MmprojFileName);
    public string DisplayName => 
        $"{Name}{(HasMmproj ? " (+mmproj)" : string.Empty)} ({Format}, {FormatSize(SizeBytes)})";

    private static string FormatSize(long sizeBytes) =>
        sizeBytes >= 1024L * 1024 * 1024
            ? $"{sizeBytes / (1024d * 1024 * 1024):0.0} GB"
            : $"{sizeBytes / (1024d * 1024):0} MB";
}

public sealed record ServerStatus(bool IsRunning, string? CurrentModel = null);
