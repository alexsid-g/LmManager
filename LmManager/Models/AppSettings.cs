namespace LmManager.Models;

public sealed class AppSettings
{
    public string ModelsDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AI", "models");
    public string LogsDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AI", "logs");
    
    public string LlamaHost { get; set; } = "127.0.0.1";
    public int LlamaPort { get; set; } = 4901;
    public string LlamaExecutable { get; set; } = "llama-server";
    
    public string MlxHost { get; set; } = "127.0.0.1";
    public int MlxPort { get; set; } = 4902;
    public string MlxExecutable { get; set; } = "mlx_lm.server";
    
    public string MlxVlmHost { get; set; } = "127.0.0.1";
    public int MlxVlmPort { get; set; } = 4903;
    public string MlxVlmExecutable { get; set; } = "mlx_vlm.server";

    public Dictionary<string, int> ModelContextSizes { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> ModelTemperatures { get; set; } = new(StringComparer.Ordinal);
}
