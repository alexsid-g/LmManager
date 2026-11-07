using LmManager.Models;

namespace LmManager.Services;

public sealed class LlamaCppProvider : LLMServerProvider
{
    private readonly AppSettings _settings;
    public LlamaCppProvider(AppSettings settings, Action<string>? logOutput = null)
        : base(logOutput)
    {
        _settings = settings;
    }

    public override string Name => "llama.cpp";
    public override string Address => $"http://{_settings.LlamaHost}:{_settings.LlamaPort}";
    public override ModelFormat SupportedModelFormat => ModelFormat.Gguf;
    protected override string Executable => _settings.LlamaExecutable;
    protected override string ExecutableName => "llama-server";
    protected override string ModelsEndpoint => "/v1/models";
    protected override string? RuntimeParametersEndpoint => "/props";
    protected override string? HealthEndpoint => "/health";
    protected override string[] ValidationArguments => ["--version"];
    public override bool Supports(ModelInfo model) => model.Format == ModelFormat.Gguf;
    protected override string[] BuildArguments(ModelInfo model)
    {
        var arguments = new List<string> { "-m", model.Path };
        arguments.AddRange([
            "--ctx-size", model.ContextSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--temp", model.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--host", _settings.LlamaHost,
            "--port", _settings.LlamaPort.ToString(System.Globalization.CultureInfo.InvariantCulture)
        ]);

        if (model.HasMmproj && model.MmprojFileName is { } mmprojFileName)
        {
            var modelDirectory = Path.GetDirectoryName(model.Path) ?? ".";
            arguments.AddRange(["--mmproj", Path.Combine(modelDirectory, mmprojFileName)]);
            arguments.AddRange(["--image-min-tokens", "2048"]);
        }
        
        arguments.AddRange(model.StartupParameters);
        return arguments.ToArray();
    }
}
