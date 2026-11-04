using LmManager.Models;

namespace LmManager.Services;

public sealed class MlxVlmProvider : LlmServerProvider
{
    private readonly AppSettings _settings;
    public MlxVlmProvider(AppSettings settings, Action<string>? logOutput = null)
        : base(logOutput)
    {
        _settings = settings;
    }

    public override string Name => "MLX-LMV";
    public override string Address => $"http://{_settings.MlxVlmHost}:{_settings.MlxVlmPort}";
    protected override string Executable => _settings.MlxVlmExecutable;
    protected override string ExecutableName => "mlx_vlm.server";
    protected override string ModelsEndpoint => "/v1/models";
    protected override string? HealthEndpoint => null;
    public override bool Supports(ModelInfo model) => model.Format == ModelFormat.Mlx;
    protected override string[] BuildArguments(ModelInfo model) =>
        ["--model", model.Path, "--host", _settings.MlxVlmHost, "--port", _settings.MlxVlmPort.ToString()];
}
