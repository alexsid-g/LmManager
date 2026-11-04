using LmManager.Models;

namespace LmManager.Services;

public sealed class MlxVlmProvider(AppSettings settings, Action<string>? logOutput = null)
    : MlxLmProvider(settings, logOutput)
{
    protected override string ProviderName => "MLX-LMV";
    protected override string Host => Settings.MlxVlmHost;
    protected override int Port => Settings.MlxVlmPort;
    protected override string ServerExecutable => Settings.MlxVlmExecutable;
    protected override string ServerExecutableName => "mlx_vlm.server";
}
