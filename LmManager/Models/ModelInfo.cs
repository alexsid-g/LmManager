namespace LmManager.Models;

public enum ModelFormat
{
    Mlx,
    Gguf
}

public enum ReasoningProfile
{
    None,
    Medium,
    Maximum,
    Unlim,
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
    public const ReasoningProfile DefaultReasoning = ReasoningProfile.Medium;

    public static IReadOnlyList<(double Value, string Label)> TemperatureOptions { get; } =
        Array.AsReadOnly<(double Value, string Label)>(
        [
            (0.3, "Development (0.3)"),
            (0.5, "Analytics (0.5)"),
            (0.7, "Regular (0.7)"),
            (0.9, "Creative (0.9)"),
        ]);

    public static IReadOnlyList<(ReasoningProfile Value, string Label)> ReasoningOptions { get; } =
        Array.AsReadOnly<(ReasoningProfile Value, string Label)>(
        [
            (ReasoningProfile.None, nameof(ReasoningProfile.None)),
            (ReasoningProfile.Medium, nameof(ReasoningProfile.Medium)),
            (ReasoningProfile.Maximum, nameof(ReasoningProfile.Maximum)),
            (ReasoningProfile.Unlim, nameof(ReasoningProfile.Unlim)),
        ]);

    public IReadOnlyList<int> ContextSizeOptions { get; } =  [32768, 65536, 131072];

    public int ContextSize { get; init; } = DefaultContextSize;
    public double Temperature { get; init; } = DefaultTemperature;
    public ReasoningProfile Reasoning { get; init; } = DefaultReasoning;
    public bool HasParamsFiles { get; init; }
    public bool HasMmproj => !string.IsNullOrWhiteSpace(MmprojFileName);

    public string DisplayName => GetDisplayName();

    public string GetDisplayName() =>
        $"{Name} ({GetFormatLabel(Format)}){(HasParamsFiles ? "*" : string.Empty)}";

    public static bool IsSupportedTemperature(double temperature) =>
        TemperatureOptions.Any(option => option.Value == temperature);

    public static bool IsSupportedReasoning(ReasoningProfile reasoning) =>
        ReasoningOptions.Any(option => option.Value == reasoning);

    public static string GetFormatLabel(ModelFormat format) =>
        format switch
        {
            ModelFormat.Gguf => "GGUF",
            ModelFormat.Mlx => "MLX",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
}

public sealed record ServerStatus(
    bool IsRunning,
    string? CurrentModel = null,
    ServerRuntimeParameters? RuntimeParameters = null);

public sealed record ServerRuntimeParameters(int? ContextSize = null, double? Temperature = null)
{
    public bool HasValues => ContextSize.HasValue || Temperature.HasValue;
}
