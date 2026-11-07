using LmManager.Models;

namespace LmManager.Services;

public sealed class LlamaCppProvider : LLMServerProvider
{
    private static readonly Dictionary<ReasoningProfile, Dictionary<string, string>> ReasoningProfiles = new()
    {
        [ReasoningProfile.None] = new()
        {
            ["--reasoning"] = "off",
            ["--reasoning-budget"] = "0",
        },

        // about 2048 * 70% / 500 = 2.86 pages of text
        [ReasoningProfile.Medium] = new()
        {
            ["--reasoning"] = "auto",
            ["--reasoning-budget"] = "2048",
        },

        // about 8192 * 70% / 500 = 11.46 pages of text
        [ReasoningProfile.Maximum] = new()
        {
            ["--reasoning"] = "on",
            ["--reasoning-budget"] = "8192",
        },

        // all available
        [ReasoningProfile.Unlim] = new()
        {
            ["--reasoning"] = "on",
        },
    };

    private static readonly HashSet<string> ParamsBlacklist = new(
        [
            "-m", "--model", "-mu", "--model-url", "--temp", "--host", "--port", "--mmproj",
            "--reasoning", "--reasoning-budget"
        ],
        StringComparer.OrdinalIgnoreCase); 

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
        var args = new Dictionary<string, string>
        { 
            { "-m", model.Path },
            { "--ctx-size", model.ContextSize.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            { "--temp", model.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            { "--host", _settings.LlamaHost },
            { "--port", _settings.LlamaPort.ToString(System.Globalization.CultureInfo.InvariantCulture) },
        };

        if (model.HasMmproj && model.MmprojFileName is { } mmprojFileName)
        {
            var modelDirectory = Path.GetDirectoryName(model.Path) ?? ".";
            args.Add("--mmproj", Path.Combine(modelDirectory, mmprojFileName));
            args.Add("--image-min-tokens", "1024");
            args.Add("--image-max-tokens", "2048");
        }

        if (!ReasoningProfiles.TryGetValue(model.Reasoning, out var reasoning))
            throw new ArgumentOutOfRangeException(nameof(model), model.Reasoning, "Unsupported reasoning profile.");
        foreach (var (name, value) in reasoning)
            args[name] = value;

        return LoadStartupParametersAndMerge(model, args, ParamsBlacklist);
    }
}
