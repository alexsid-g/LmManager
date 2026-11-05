using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using LmManager.Models;
using LmManager.Services;

namespace LmManager.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly SettingsStore _settingsStore = new();
    private readonly ModelScanner _modelScanner = new();
    private readonly AppLogger _logger;
    private readonly AppSettings _settings;
    private readonly List<LLMServerProvider> _providers = [];
    private volatile bool _refreshing;
    private volatile bool _refreshPending;
    private volatile bool _scanning;
    private volatile bool _initialized;

    [ObservableProperty]
    private string _modelsDirectory;

    [ObservableProperty]
    private string _statusMessage = "Starting...";

    public bool HasError { get; private set; }

    public ObservableCollection<ModelInfo> Models { get; } = [];
    public ObservableCollection<ServerViewModel> Servers { get; } = [];
    public event Action? StateChanged;

    public MainViewModel()
    {
        _settings = _settingsStore.Load();
        _modelsDirectory = _settings.ModelsDirectory;
        _logger = new AppLogger(_settings.LogsDirectory, message =>
        {
            StatusMessage = message;
            HasError = true;
            StateChanged?.Invoke();
        });
        _logger.Write($"Application started. Settings file: {_settingsStore.SettingsPath}");

        _providers.Add(new LlamaCppProvider(_settings, message => _logger.Write(message)));
        _providers.Add(new MlxLmProvider(_settings, message => _logger.Write(message)));
        foreach (var provider in _providers)
            Servers.Add(new ServerViewModel(provider));
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;
        _initialized = true;
        await ScanModelsAsync();
        await RefreshAsync();
    }

    public async Task ChooseModelsDirectoryAsync(Window owner)
    {
        IReadOnlyList<IStorageFolder> folders;
        try
        {
            folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select models folder",
                AllowMultiple = false
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            StatusMessage = $"Could not open folder picker: {ex.Message}";
            HasError = true;
            _logger.Write(StatusMessage);
            StateChanged?.Invoke();
            return;
        }

        var folder = folders.FirstOrDefault();
        if (folder is null)
            return;

        var selectedPath = folder.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            StatusMessage = "The selected folder is not available as a local path.";
            HasError = true;
            _logger.Write(StatusMessage);
            StateChanged?.Invoke();
            return;
        }

        var fullPath = Path.GetFullPath(selectedPath);
        _settings.ModelsDirectory = fullPath;
        try
        {
            _settingsStore.Save(_settings);
            ModelsDirectory = fullPath;
            HasError = false;
            _logger.Write($"Saved models folder: {ModelsDirectory}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _settings.ModelsDirectory = ModelsDirectory;
            StatusMessage = $"Could not save settings: {ex.Message}";
            HasError = true;
            _logger.Write(StatusMessage);
            StateChanged?.Invoke();
            return;
        }
        await ScanModelsAsync();
    }

    public async Task ScanModelsAsync()
    {
        if (_scanning)
            return;
        _scanning = true;
        try
        {
            if (!Directory.Exists(ModelsDirectory))
            {
                Models.Clear();
                StatusMessage = $"Models folder not found: {ModelsDirectory}. Choose a folder from the menu.";
                HasError = true;
                _logger.Write(StatusMessage);
                StateChanged?.Invoke();
                return;
            }

            StatusMessage = $"Scanning: {ModelsDirectory}";
            StateChanged?.Invoke();
            var result = await Task.Run(() => _modelScanner.Scan(ModelsDirectory));
            Models.Clear();
            foreach (var model in result.Models)
                Models.Add(model);

            StatusMessage = result.Errors.Count == 0
                ? $"Models found: {Models.Count}"
                : $"Models found: {Models.Count}; inaccessible items skipped: {result.Errors.Count}";
            HasError = result.Errors.Count > 0;
            _logger.Write($"Scan completed: found {Models.Count} models in {ModelsDirectory}.");
            foreach (var error in result.Errors)
                _logger.Write($"Scan error: {error}");
            StateChanged?.Invoke();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
            HasError = true;
            _logger.Write(StatusMessage);
            StateChanged?.Invoke();
        }
        finally
        {
            _scanning = false;
        }
    }

    public async Task RefreshAsync()
    {
        if (_refreshing)
        {
            // A refresh is already in progress; request that it run once more
            // when the current one finishes so no update is lost.
            _refreshPending = true;
            return;
        }

        _refreshing = true;
        try
        {
            do
            {
                _refreshPending = false;
                foreach (var server in Servers)
                    await server.RefreshAsync();
                StateChanged?.Invoke();
            }
            while (_refreshPending);
        }
        finally
        {
            _refreshing = false;
        }
    }

    public async Task StartModelAsync(ServerViewModel server, ModelInfo model)
    {
        if (!server.Supports(model))
        {
            StatusMessage = $"{server.Name} does not support the {model.Format} format.";
            HasError = true;
            StateChanged?.Invoke();
            return;
        }

        try
        {
            var status = await server.Provider.GetStatusAsync();
            if (status.IsRunning && !server.Provider.IsManagedProcessRunning)
            {
                StatusMessage = $"{server.Name} is already running outside this app. Stop it from the server menu before loading another model.";
                HasError = true;
                StateChanged?.Invoke();
                return;
            }

            await server.Provider.StartAsync(model);
            StatusMessage = $"Started model {model.Name} on {server.Name}.";
            HasError = false;
            _logger.Write(StatusMessage);
            StateChanged?.Invoke();

            if (await WaitForServerStateAsync(server, static currentServer => currentServer.IsModelLoaded))
            {
                HasError = false;
                StatusMessage = $"Loaded model {model.Name} on {server.Name}.";
                StateChanged?.Invoke();
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            StatusMessage = $"Could not start {server.Name}: {ex.Message}";
            HasError = true;
            _logger.Write(StatusMessage);
        }
        StateChanged?.Invoke();
    }

    public async Task StopServerAsync(ServerViewModel server)
    {
        try
        {
            await server.Provider.StopAsync();
            HasError = false;
            StatusMessage = $"Stopping {server.Name}...";
            StateChanged?.Invoke();

            if (await WaitForServerStateAsync(server, static currentServer => !currentServer.IsRunning))
            {
                StatusMessage = $"Stopped {server.Name}.";
                HasError = false;
                _logger.Write(StatusMessage);
                StateChanged?.Invoke();
                return;
            }

            StatusMessage = $"Could not confirm that {server.Name} stopped.";
            HasError = true;
            _logger.Write(StatusMessage);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception or PlatformNotSupportedException)
        {
            StatusMessage = ex.Message;
            HasError = true;
            _logger.Write(StatusMessage);
        }
        StateChanged?.Invoke();
    }

    private async Task<bool> WaitForServerStateAsync(
        ServerViewModel server,
        Func<ServerViewModel, bool> isDesiredState)
    {
        for (var i = 1; i <= 3; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(i));
            await server.RefreshAsync();

            StateChanged?.Invoke();
            if (isDesiredState(server))
                return true;
        }

        return false;
    }

    public void OpenLogFolder() => _logger.OpenLogFolder();

    public void Dispose()
    {
        foreach (var provider in _providers)
            provider.Dispose();
        _logger.Dispose();
    }

    public bool HasLoadedModel => Servers.Any(server => server.IsModelLoaded);
}

public sealed class ServerViewModel(LLMServerProvider provider)
{
    public LLMServerProvider Provider { get; } = provider;
    public string Name => Provider.Name;
    public string Address => Provider.Address;
    public string? CurrentModel { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsModelLoaded => !string.IsNullOrWhiteSpace(CurrentModel);
    public bool IsManagedProcessRunning => Provider.IsManagedProcessRunning;
    // Stopping a process started by this app works on any platform, but stopping an
    // externally started server uses macOS-only tooling (lsof/ps/kill), so it is only
    // offered there.
    public bool CanStop => IsManagedProcessRunning || (IsRunning && OperatingSystem.IsMacOS());

    public bool Supports(ModelInfo model) => Provider.Supports(model);

    public async Task RefreshAsync()
    {
        var status = await Provider.GetStatusAsync();
        IsRunning = status.IsRunning;
        CurrentModel = status.CurrentModel;
    }
}
