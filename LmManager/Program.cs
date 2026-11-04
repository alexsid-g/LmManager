using Avalonia;

namespace LmManager;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        ConfigureHomebrewPath();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void ConfigureHomebrewPath()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        const string homebrewBin = "/opt/homebrew/bin";
        if (!Directory.Exists(homebrewBin))
        {
            Console.WriteLine($"Homebrew directory '{homebrewBin}' does not exist.");
            return;
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathEntries = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (pathEntries.Any(entry =>
                string.Equals(
                    Path.TrimEndingDirectorySeparator(entry),
                    homebrewBin,
                    StringComparison.Ordinal)))
        {
            return;
        }

        Environment.SetEnvironmentVariable(
            "PATH",
            string.IsNullOrEmpty(path) ? homebrewBin : $"{homebrewBin}{Path.PathSeparator}{path}");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
