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
        Directory.CreateDirectory(directory);
        LogFilePath = Path.Combine(directory, "lm-manager.log");
        SelfLog.Enable(reportError);
        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                LogFilePath,
                fileSizeLimitBytes: 1024 * 1024,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 5)
            .CreateLogger();
        _logger.Information("Log file: {Path}", LogFilePath);
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
