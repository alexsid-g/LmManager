using LmManager.Models;

namespace LmManager.Services;

public class MlxLmProvider : LLMServerProvider
{
    private static readonly HashSet<string> ParamsBlacklist = new(
        ["--model", "--host", "--port"],
        StringComparer.OrdinalIgnoreCase);

    protected readonly AppSettings Settings;

    public MlxLmProvider(AppSettings settings, Action<string>? logOutput = null)
        : base(logOutput)
    {
        Settings = settings;
    }

    protected virtual string ProviderName => "MLX-LM";
    protected virtual string Host => Settings.MlxHost;
    protected virtual int Port => Settings.MlxPort;
    protected virtual string ServerExecutable => Settings.MlxExecutable;
    protected virtual string ServerExecutableName => "mlx_lm.server";

    public override string Name => ProviderName;
    public override string Address => $"http://{Host}:{Port}";
    public override ModelFormat SupportedModelFormat => ModelFormat.Mlx;
    protected override string Executable => ServerExecutable;
    protected override string ExecutableName => ServerExecutableName;
    protected override string ModelsEndpoint => "/v1/models";
    protected override string? HealthEndpoint => null;
    public override bool Supports(ModelInfo model) => model.Format == ModelFormat.Mlx;
    protected override string[] BuildArguments(ModelInfo model)
    {
        Dictionary<string, string> args = new()
        {
            { "--model", model.Path },
            { "--use-default-chat-template", string.Empty },
            { "--host", Host },
            { "--port", Port.ToString(System.Globalization.CultureInfo.InvariantCulture)},
        };

        return LoadStartupParametersAndMerge(model, args, ParamsBlacklist);
    }
        
}
