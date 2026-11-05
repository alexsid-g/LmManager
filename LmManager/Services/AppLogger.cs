using Serilog;
using Serilog.Core;
using Serilog.Debugging;

namespace LmManager.Services;

public sealed class AppLogger : IDisposable
{
    private readonly Logger _logger;
    public string LogFilePath { get; }

    public AppLogger(string directory, Action<string> reportError)
    {
        var logDirectory = ResolveDirectory(directory);
        Directory.CreateDirectory(logDirectory);
        LogFilePath = Path.Combine(logDirectory, "lm-manager.log");
        SelfLog.Enable(reportError);
        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                LogFilePath,
                buffered: false,
                fileSizeLimitBytes: 1024 * 1024,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 5,
                shared: true)
            .CreateLogger();
        _logger.Information("Log file: {Path}", LogFilePath);
    }

    private static string ResolveDirectory(string directory)
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (directory == "~")
            directory = homeDirectory;
        else if (directory.StartsWith($"~{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                 directory.StartsWith($"~{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
            directory = Path.Combine(homeDirectory, directory[2..]);

        return Path.GetFullPath(directory);
    }

    public void Write(string message) => _logger.Information("{Message}", message);

    public void OpenLogFolder() => OpenPath(Path.GetDirectoryName(LogFilePath)!);

    private static void OpenPath(string path)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"{path.TrimEnd('/')}",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processInfo);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open log folder: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _logger.Dispose();
        SelfLog.Disable();
    }
}
