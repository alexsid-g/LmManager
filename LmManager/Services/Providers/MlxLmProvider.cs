using LmManager.Models;

namespace LmManager.Services;

public sealed class MlxLmProvider : LLMServerProvider
{
    private static readonly HashSet<string> ParamsBlacklist = new(
        ["--model", "--host", "--port"],
        StringComparer.OrdinalIgnoreCase);

    private readonly AppSettings Settings;

    public MlxLmProvider(AppSettings settings, Action<string>? logOutput = null)
        : base(logOutput)
    {
        Settings = settings;
    }

    public override string Name => "MLX-LM";
    public override string Address => $"http://{Settings.MlxHost}:{Settings.MlxPort}";
    public override ModelFormat SupportedModelFormat => ModelFormat.Mlx;
    protected override string Executable => Settings.MlxExecutable;
    protected override string ExecutableName => "mlx_lm.server";
    protected override string ModelsEndpoint => "/v1/models";
    protected override string? HealthEndpoint => null;
    public override bool Supports(ModelInfo model) => model.Format == ModelFormat.Mlx;
    protected override string[] BuildArguments(ModelInfo model)
    {
        Dictionary<string, string> args = new()
        {
            { "--model", model.Path },
            { "--use-default-chat-template", string.Empty },
            { "--host", Settings.MlxHost },
            { "--port", Settings.MlxPort.ToString(System.Globalization.CultureInfo.InvariantCulture)},
            { "--top-p", LLMConsts.TopP },
            { "--top-k", LLMConsts.TopK },
            { "--min-p", LLMConsts.MinP },
            // Some test params for M5 Pro architecture
            { "--max-tokens", "8192" },
            { "--prefill-step-size", "4096" },
            { "--prompt-cache-size", "4" },
            { "--kv-bits", "8" },
            { "--kv-group-size", "64" },
            { "--quantized-kv-start", "4096" },
        };

        return LoadStartupParametersAndMerge(model, args, ParamsBlacklist);
    }
        
}
