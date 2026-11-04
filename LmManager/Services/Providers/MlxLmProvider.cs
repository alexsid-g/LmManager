using LmManager.Models;

namespace LmManager.Services;

public sealed class MlxLmProvider : LlmServerProvider
{
    private readonly AppSettings _settings;
    public MlxLmProvider(AppSettings settings) => _settings = settings;

    public override string Name => "MLX-LM";
    public override string Address => $"http://{_settings.MlxHost}:{_settings.MlxPort}";
    protected override string Executable => _settings.MlxExecutable;
    protected override string ExecutableName => "mlx_lm.server";
    protected override string ModelsEndpoint => "/v1/models";
    protected override string? HealthEndpoint => null;
    public override bool Supports(ModelInfo model) => model.Format == ModelFormat.Mlx;
    protected override string[] BuildArguments(ModelInfo model) =>
        ["--model", model.Path, "--host", _settings.MlxHost, "--port", _settings.MlxPort.ToString()];
}
