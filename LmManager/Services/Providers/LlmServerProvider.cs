using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using LmManager.Models;

namespace LmManager.Services;

public abstract class LLMServerProvider : IDisposable
{
    private static readonly HttpClient HttpClient = new();
    private readonly Action<string>? _logOutput;
    private Process? _managedProcess;

    protected LLMServerProvider(Action<string>? logOutput = null)
    {
        _logOutput = logOutput;
    }

    public abstract string Name { get; }
    public abstract string Address { get; }
    protected abstract string Executable { get; }
    protected abstract string ExecutableName { get; }
    protected abstract string ModelsEndpoint { get; }
    protected virtual string[] ValidationArguments => ["--help"];
    /// <summary>
    /// Optional liveness endpoint. When non-null the server is considered running
    /// only when the response body has "status": "ok". When null, liveness is derived
    /// from the models endpoint.
    /// </summary>
    protected abstract string? HealthEndpoint { get; }

    public bool IsManagedProcessRunning => _managedProcess is { HasExited: false };
    public abstract bool Supports(ModelInfo model);
    protected abstract string[] BuildArguments(ModelInfo model);

    public async Task<ServerStatus> GetStatusAsync()
    {
        // llama.cpp exposes a dedicated /health endpoint: treat "status": "ok" as running.
        if (!string.IsNullOrWhiteSpace(HealthEndpoint))
        {
            if (!await CheckHealthAsync())
                return new ServerStatus(false);
            return new ServerStatus(true, await GetLoadedModelIdAsync());
        }

        // MLX-LM has no dedicated health endpoint: liveness comes from /v1/models.
        return await GetStatusViaModelsApiAsync();
    }

    private async Task<bool> CheckHealthAsync()
    {
        var endpoint = new Uri(new Uri(Address), HealthEndpoint);
        using var json = await GetJsonAsync(endpoint);
        if (json is null)
            return false;

        return json.RootElement.TryGetProperty("status", out var status) &&
               status.ValueKind == JsonValueKind.String &&
               string.Equals(status.GetString(), "ok", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string?> GetLoadedModelIdAsync()
    {
        var endpoint = new Uri(new Uri(Address), ModelsEndpoint);
        using var json = await GetJsonAsync(endpoint);
        return json is null ? null : GetModelId(json.RootElement);
    }

    private async Task<ServerStatus> GetStatusViaModelsApiAsync()
    {
        var endpoint = new Uri(new Uri(Address), ModelsEndpoint);
        using var json = await GetJsonAsync(endpoint);
        if (json is null)
            return new ServerStatus(false);

        return new ServerStatus(true, GetModelId(json.RootElement));
    }

    private async Task<JsonDocument?> GetJsonAsync(Uri endpoint)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var response = await HttpClient.GetAsync(
                endpoint,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var content = await response.Content.ReadAsStreamAsync(timeout.Token);
            return await JsonDocument.ParseAsync(content, cancellationToken: timeout.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or InvalidOperationException or IOException or JsonException or UriFormatException or ArgumentException)
        {
            return null;
        }
    }

    private static string? GetModelId(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var model in data.EnumerateArray())
        {
            if (model.ValueKind != JsonValueKind.Object ||
                !model.TryGetProperty("id", out var id) ||
                id.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(id.GetString()))
            {
                continue;
            }

            return id.GetString();
        }

        return null;
    }

    public async Task StartAsync(ModelInfo model)
    {
        if (!Supports(model))
            throw new InvalidOperationException($"{Name} does not support the {model.Format} format.");

        var resolvedExecutable = ExecutableResolver.Find(Executable, ExecutableName)
            ?? throw new InvalidOperationException(
                $"Could not find an executable file for '{Executable}'. Check that it exists, is executable, and is available in PATH.");
        await ValidateExecutableAsync(resolvedExecutable);

        if (_managedProcess is { HasExited: false })
        {
            await StopAsync();
            await _managedProcess!.WaitForExitAsync();
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = resolvedExecutable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in BuildArguments(model))
            startInfo.ArgumentList.Add(argument);

        _logOutput?.Invoke(
            $"[{Name}] Starting model process: executable={JsonSerializer.Serialize(resolvedExecutable)}, arguments={JsonSerializer.Serialize(startInfo.ArgumentList)}");

        try
        {
            _managedProcess = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Starting '{resolvedExecutable}' returned no process.");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Found executable '{resolvedExecutable}', but could not start it. Check its format, permissions, and required runtime.",
                ex);
        }

        _ = ReadProcessStreamsAsync(_managedProcess);
    }

    private async Task ValidateExecutableAsync(string executablePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in ValidationArguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new InvalidOperationException($"Starting '{executablePath}' returned no process.");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Found executable '{executablePath}', but could not run it. Check its permissions, format, and required runtime.",
                ex);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException ex)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await Task.WhenAll(standardOutputTask, standardErrorTask);
            throw new IOException(
                $"Executable '{executablePath}' did not finish its '{string.Join(' ', ValidationArguments)}' check within 15 seconds.",
                ex);
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        if (process.ExitCode != 0)
        {
            var details = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
            throw new InvalidOperationException(
                $"Executable '{executablePath}' failed its '{string.Join(' ', ValidationArguments)}' check with exit code {process.ExitCode}." +
                (string.IsNullOrWhiteSpace(details) ? string.Empty : $" {details.Trim()}"));
        }
    }

    private async Task ReadProcessStreamsAsync(Process process)
    {
        try
        {
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    _logOutput?.Invoke($"[{Name}] stdout: {args.Data}");
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    _logOutput?.Invoke($"[{Name}] stderr: {args.Data}");
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or ObjectDisposedException or OperationCanceledException or NotSupportedException)
        {
            _logOutput?.Invoke($"[{Name}] Could not read the server process output: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        if (_managedProcess is { HasExited: false } process)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            return;
        }

        await StopExternalProcessAsync();
    }

    private async Task StopExternalProcessAsync()
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Stopping an externally started server is currently supported on macOS only.");

        var endpoint = new Uri(Address);
        var listenerOutput = await RunCommandAsync(
            "lsof",
            "-nP",
            $"-iTCP:{endpoint.Port}",
            "-sTCP:LISTEN",
            "-t");
        var processIds = listenerOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => int.TryParse(line, out var processId) ? processId : 0)
            .Where(processId => processId > 0)
            .Distinct()
            .ToArray();

        if (processIds.Length == 0)
            throw new InvalidOperationException($"No process is listening on port {endpoint.Port}.");

        var matchingProcesses = new List<(int ProcessId, string CommandLine)>();
        foreach (var processId in processIds)
        {
            var commandLine = await RunCommandAsync("ps", "-p", processId.ToString(), "-o", "command=");
            if (IsConfiguredExecutable(commandLine))
                matchingProcesses.Add((processId, commandLine));
        }

        if (matchingProcesses.Count == 0)
            throw new InvalidOperationException(
                $"A process is listening on port {endpoint.Port}, but it does not match the configured executable '{Executable}'. No process was stopped.");

        foreach (var (processId, _) in matchingProcesses)
        {
            await RunCommandAsync("kill", "-TERM", processId.ToString());
            try
            {
                using var process = Process.GetProcessById(processId);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (ArgumentException)
            {
                // The process exited before it could be opened or while waiting.
            }
            catch (OperationCanceledException ex)
            {
                throw new IOException($"Server process {processId} did not stop after receiving SIGTERM.", ex);
            }
        }
    }

    private bool IsConfiguredExecutable(string commandLine)
    {
        var executableName = Path.GetFileName(Executable);
        return commandLine
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim('"', '\''))
            .Any(token =>
                string.Equals(token, Executable, StringComparison.Ordinal) ||
                string.Equals(Path.GetFileName(token), executableName, StringComparison.Ordinal));
    }

    private static async Task<string> RunCommandAsync(string executable, params string[] arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        if (!process.Start())
            throw new IOException($"Could not start '{executable}'.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException ex)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await Task.WhenAll(outputTask, errorTask);
            throw new IOException($"Command '{executable}' timed out.", ex);
        }

        await Task.WhenAll(outputTask, errorTask);
        if (process.ExitCode != 0)
        {
            var error = await errorTask;
            throw new IOException(string.IsNullOrWhiteSpace(error)
                ? $"Command '{executable}' exited with code {process.ExitCode}."
                : error.Trim());
        }

        return await outputTask;
    }

    public virtual void Dispose()
    {
        if (_managedProcess is { HasExited: false })
            _managedProcess.Kill(entireProcessTree: true);
        _managedProcess?.Dispose();
    }
}
