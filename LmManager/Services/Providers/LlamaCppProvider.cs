using LmManager.Models;

namespace LmManager.Services;

public sealed class LlamaCppProvider : LlmServerProvider
{
    private readonly AppSettings _settings;
    public LlamaCppProvider(AppSettings settings, Action<string>? logOutput = null)
        : base(logOutput)
    {
        _settings = settings;
    }

    public override string Name => "llama.cpp";
    public override string Address => $"http://{_settings.LlamaHost}:{_settings.LlamaPort}";
    protected override string Executable => _settings.LlamaExecutable;
    protected override string ExecutableName => "llama-server";
    protected override string ModelsEndpoint => "/v1/models";
    protected override string? HealthEndpoint => "/health";
    public override bool Supports(ModelInfo model) => model.Format == ModelFormat.Gguf;
    protected override string[] BuildArguments(ModelInfo model) =>
        ["-m", model.Path, "--host", _settings.LlamaHost, "--port", _settings.LlamaPort.ToString()];
}
