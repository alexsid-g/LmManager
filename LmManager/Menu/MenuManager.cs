using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using LmManager.Models;
using LmManager.Utilities;
using LmManager.ViewModels;
using System.Diagnostics;

namespace LmManager.Menu;

public sealed class MenuManager(
    IClassicDesktopStyleApplicationLifetime desktop,
    Window owner,
    NativeMenu rootMenu,
    MainViewModel viewModel)
{
    private readonly Dictionary<string, NativeMenuItem> _topItems = new();
    private readonly Dictionary<string, NativeMenu> _serverModelMenus = new();
    private readonly Dictionary<string, NativeMenuItem> _serverModelStatusItems = new();
    private readonly Dictionary<string, NativeMenuItem> _serverStopItems = new();
    private NativeMenu? _foundModelsMenu;
    private NativeMenuItem? _foundModelsItem;
    private NativeMenuItem? _currentStatusAddressItem;
    private NativeMenuItem? _currentStatusModelItem;
    private NativeMenuItem? _currentStatusParametersItem;
    private NativeMenuItem? _currentStatusMessageItem;
    private readonly List<NativeMenuItem> _llamaCppUrlItems = [];
    private bool _built;

    public void Build()
    {
        if (_built)
            return;

        CreateTopLevelItems();
        _built = true;

        Refresh();
        Application.Current?.ActualThemeVariantChanged += (_, _) =>
        {
            MenuIcons.ClearMenuIconsCache();
            Rebuild();
        };
    }

    public void Refresh()
    {
        if (!_built)
            return;

        _topItems[MenuKeys.Title].Header = $"{MenuText.TitlePrefix}";
        _topItems[MenuKeys.Models].Header = $"{MenuText.ModelsFolderPrefix}: {viewModel.ModelsDirectory}";
        RefreshFoundModelsMenu();

        var addresses = viewModel.Servers
            .Where(server => server.IsRunning)
            .Select(server => server.Address)
            .Distinct()
            .ToArray();
        _currentStatusAddressItem!.Header = $"{MenuText.ServerAddressLabel}: {FormatStatusValue(addresses)}";
        _currentStatusAddressItem.IsEnabled = addresses.Length > 0;

        var models = viewModel.Servers
            .Where(server => server.IsRunning && !string.IsNullOrWhiteSpace(server.CurrentModel))
            .Select(FormatCurrentModel)
            .Distinct()
            .ToArray();
        _currentStatusModelItem!.Header = $"{MenuText.ModelLabel}: {FormatStatusValue(models)}";
        _currentStatusModelItem.IsEnabled = models.Length > 0;

        var modelParameters = GetRunningModelParameters().ToArray();
        _currentStatusParametersItem!.Header =
            $"{MenuText.ModelParametersLabel}: {FormatModelParameters(modelParameters)}";
        _currentStatusParametersItem.IsEnabled = modelParameters.Length > 0;

        _currentStatusMessageItem!.Header = $"{MenuText.StatusMessageLabel}: {viewModel.StatusMessage}";
        var canOpenLlamaCppUrls = GetLlamaCppServer()?.IsRunning == true;
        foreach (var item in _llamaCppUrlItems)
            item.IsEnabled = canOpenLlamaCppUrls;

        foreach (var server in viewModel.Servers)
        {
            _topItems[$"{MenuKeys.ServerPrefix}{server.Name}"].Header =
                $"{server.Name}{MenuText.StatusSeparator}{(server.IsRunning ? MenuText.Running : MenuText.Stopped)} ({server.Address})";

            var modelStatus = _serverModelStatusItems[server.Name];
            modelStatus.IsVisible = !string.IsNullOrWhiteSpace(server.CurrentModel);
            modelStatus.Header = $"{MenuText.ModelLabel}: {FormatCurrentModel(server)}";

            var stopItem = _serverStopItems[server.Name];
            stopItem.Header = server.IsManagedProcessRunning
                ? MenuText.StopServer
                : (OperatingSystem.IsMacOS() ? MenuText.StopExternalServer : MenuText.StopExternalServerOnly);
            stopItem.IsEnabled = server.CanStop;

            RefreshModelMenu(server, _serverModelMenus[server.Name]);
        }
    }

    private void CreateTopLevelItems()
    {
        rootMenu.Items.Clear();

        var title = new NativeMenuItem
        {
            Header = "",
            IsEnabled = true,
            Icon = GetIcon(MenuIcons.Copy_Icon),
        };
        title.Click += async (_, _) => await CopyTextToClipboardAsync(title.Header);
        _topItems[MenuKeys.Title] = title;

        var modelsItem = new NativeMenuItem { Header = "", Icon = GetIcon(MenuIcons.Folder_Icon) };
        var modelsMenu = new NativeMenu();
        modelsMenu.Items.Add(CreateAction(
            MenuText.ChooseFolder,
            GetIcon(MenuIcons.FolderOpen_Icon),
            async () => await viewModel.ChooseModelsDirectoryAsync(owner)));
        modelsMenu.Items.Add(CreateAction(
            MenuText.ScanNow,
            GetIcon(MenuIcons.Search_Icon),
            async () => await viewModel.ScanModelsAsync()));
        modelsMenu.Items.Add(new NativeMenuItemSeparator());

        var foundModelsMenu = new NativeMenu();
        _foundModelsItem = new NativeMenuItem { 
            Menu = foundModelsMenu, 
            Icon = GetIcon(MenuIcons.Info_Icon),
        };
        _foundModelsMenu = foundModelsMenu;
        modelsMenu.Items.Add(_foundModelsItem);
        modelsItem.Menu = modelsMenu;
        _topItems[MenuKeys.Models] = modelsItem;

        var currentStatusItem = new NativeMenuItem
        {
            Header = MenuText.CurrentStatus,
            Icon = GetIcon(MenuIcons.Info_Icon)
        };
        var currentStatusMenu = new NativeMenu();
        _currentStatusAddressItem = CreateCopyStatusItem(() => GetRunningServerAddresses());
        _currentStatusModelItem = CreateCopyStatusItem(() => GetRunningModelNames());
        _currentStatusParametersItem = CreateCopyStatusItem(GetRunningModelParametersForClipboard);
        _currentStatusMessageItem = CreateCopyStatusItem(() => viewModel.StatusMessage);
        currentStatusMenu.Items.Add(_currentStatusAddressItem);
        currentStatusMenu.Items.Add(_currentStatusModelItem);
        currentStatusMenu.Items.Add(_currentStatusParametersItem);
        currentStatusMenu.Items.Add(_currentStatusMessageItem);
        currentStatusMenu.Items.Add(new NativeMenuItemSeparator());
        currentStatusMenu.Items.Add(CreateLlamaCppUrlItem(MenuText.OpenServer, "/"));
        currentStatusMenu.Items.Add(CreateLlamaCppUrlItem(MenuText.OpenModel, "/v1/models"));
        currentStatusMenu.Items.Add(CreateLlamaCppUrlItem(MenuText.OpenParams, "/props"));
        currentStatusItem.Menu = currentStatusMenu;
        _topItems[MenuKeys.CurrentStatus] = currentStatusItem;

        rootMenu.Items.Add(title);
        rootMenu.Items.Add(modelsItem);
        rootMenu.Items.Add(currentStatusItem);
        rootMenu.Items.Add(new NativeMenuItemSeparator());

        foreach (var server in viewModel.Servers)
        {
            CreateServersMenu(server);
        }

        rootMenu.Items.Add(new NativeMenuItemSeparator());

        var showLog = new NativeMenuItem { Header = MenuText.ShowLog, Icon = GetIcon(MenuIcons.Document_Icon) };
        showLog.Click += (_, _) => viewModel.OpenLogFolder();
        _topItems[MenuKeys.ShowLog] = showLog;

        var exitItem = new NativeMenuItem { Header = MenuText.Quit, Icon = GetIcon(MenuIcons.Dismiss_Icon) };
        exitItem.Click += (_, _) => desktop.Shutdown();
        _topItems[MenuKeys.Quit] = exitItem;

        rootMenu.Items.Add(showLog);
        rootMenu.Items.Add(exitItem);
    }

    private void Rebuild()
    {
        _topItems.Clear();
        _serverModelMenus.Clear();
        _serverModelStatusItems.Clear();
        _serverStopItems.Clear();
        _foundModelsMenu = null;
        _foundModelsItem = null;
        _currentStatusAddressItem = null;
        _currentStatusModelItem = null;
        _currentStatusParametersItem = null;
        _currentStatusMessageItem = null;
        _llamaCppUrlItems.Clear();
        CreateTopLevelItems();
        Refresh();
    }

    private ServerViewModel? GetLlamaCppServer() =>
        viewModel.Servers.FirstOrDefault(server =>
            string.Equals(server.Name, "llama.cpp", StringComparison.Ordinal));

    private NativeMenuItem CreateLlamaCppUrlItem(string header, string path)
    {
        var item = new NativeMenuItem { Header = header };
        item.Click += (_, _) => OpenLlamaCppUrl(path);
        _llamaCppUrlItems.Add(item);
        return item;
    }

    private void OpenLlamaCppUrl(string path)
    {
        var server = GetLlamaCppServer();
        if (server is null || !server.IsRunning)
            return;

        if (!Uri.TryCreate(server.Address, UriKind.Absolute, out var serverUri) ||
            (serverUri.Scheme != Uri.UriSchemeHttp && serverUri.Scheme != Uri.UriSchemeHttps) ||
            !Uri.TryCreate(serverUri, path, out var targetUri))
        {
            viewModel.ReportError(
                $"Could not open the {server.Name} page: invalid server address.",
                new InvalidOperationException($"Invalid server address '{server.Address}'."));
            return;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo(targetUri.AbsoluteUri)
            {
                UseShellExecute = true
            });

            if (process is null)
            {
                viewModel.ReportError(
                    $"Could not open the {server.Name} page.",
                    new InvalidOperationException("Starting the default browser returned no process."));
                return;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or ArgumentException or NotSupportedException)
        {
            viewModel.ReportError($"Could not open the {server.Name} page.", ex);
        }
    }

    private NativeMenuItem CreateCopyStatusItem(Func<string> getValue)
    {
        var item = new NativeMenuItem
        {
            Header = "",
            Icon = GetIcon(MenuIcons.Copy_Icon)
        };
        item.Click += async (_, _) => await CopyTextToClipboardAsync(getValue());
        return item;
    }

    private string GetRunningServerAddresses() =>
        string.Join(Environment.NewLine, viewModel.Servers
            .Where(server => server.IsRunning)
            .Select(server => server.Address)
            .Distinct());

    private string GetRunningModelNames() =>
        string.Join(Environment.NewLine, viewModel.Servers
            .Where(server => server.IsRunning && !string.IsNullOrWhiteSpace(server.CurrentModel))
            .Select(FormatCurrentModel)
            .Distinct());

    private IEnumerable<(ServerViewModel Server, int? ContextSize, double? Temperature, ReasoningProfile? Reasoning)> GetRunningModelParameters() =>
        viewModel.Servers
            .Where(server => server.IsRunning &&
                (server.StartedModel is not null ||
                 server.RuntimeParameters?.HasValues == true ||
                 FindCurrentModel(server) is not null))
            .Select(server =>
            {
                var model = server.StartedModel ?? FindCurrentModel(server);
                var contextSize = server.RuntimeParameters?.ContextSize ?? model?.ContextSize;
                var temperature = server.RuntimeParameters?.Temperature ?? model?.Temperature;
                return (Server: server, ContextSize: contextSize, Temperature: temperature, Reasoning: model?.Reasoning);
            });

    private string GetRunningModelParametersForClipboard() =>
        string.Join(Environment.NewLine, GetRunningModelParameters()
            .Select(entry =>
                $"{FormatCurrentModel(entry.Server)}{Environment.NewLine}" +
                $"{MenuText.ContextSizePrefix}: {FormatExactValue(entry.ContextSize)}{Environment.NewLine}" +
                $"{MenuText.TemperaturePrefix}: {FormatTemperature(entry.Temperature)}{Environment.NewLine}" +
                $"{MenuText.ReasoningPrefix}: {FormatReasoning(entry.Reasoning)}"));

    private string FormatModelParameters(
        IReadOnlyCollection<(ServerViewModel Server, int? ContextSize, double? Temperature, ReasoningProfile? Reasoning)> modelParameters) =>
        modelParameters.Count == 0
            ? MenuText.ValueUnavailable
            : string.Join(", ", modelParameters.Select(entry =>
                $"ctx={FormatContextSize(entry.ContextSize)}, " +
                $"temp={FormatTemperature(entry.Temperature)}, " +
                $"reasoning={FormatReasoning(entry.Reasoning)}"));

    private static string FormatContextSize(int? value) =>
        value is int contextSize
            ? FormatUtilities.FormatContextSize(contextSize)
            : MenuText.ValueUnavailable;

    private static string FormatExactValue(int? value) =>
        value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? MenuText.ValueUnavailable;

    private static string FormatTemperature(double? value) =>
        value?.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) ?? MenuText.ValueUnavailable;

    private static string FormatReasoning(ReasoningProfile? value) =>
        value?.ToString() ?? MenuText.ValueUnavailable;

    private string FormatCurrentModel(ServerViewModel server)
    {
        var currentModel = server.CurrentModel!;
        var knownModel = server.StartedModel ?? FindCurrentModel(server);
        return knownModel?.GetDisplayName() ??
            $"{GetModelName(currentModel, server.SupportedModelFormat)} " +
            $"({ModelInfo.GetFormatLabel(server.SupportedModelFormat)})";
    }

    private ModelInfo? FindCurrentModel(ServerViewModel server)
    {
        var currentModel = server.CurrentModel;
        if (string.IsNullOrWhiteSpace(currentModel))
            return null;

        var modelName = GetModelName(currentModel, server.SupportedModelFormat);
        return viewModel.Models.FirstOrDefault(model =>
            string.Equals(model.Path, currentModel, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(model.Name, currentModel, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(model.Name, modelName, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetModelName(string modelId, ModelFormat format)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            return MenuText.ValueUnavailable;

        // Assume it's a file name or path
        return Path.GetFileNameWithoutExtension(modelId);
    }

    private static string FormatStatusValue(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? MenuText.ValueUnavailable : string.Join(", ", values);

    private void CreateServersMenu(ServerViewModel server)
    {
        var serverMenu = new NativeMenu();
        var modelStatus = new NativeMenuItem
        {
            Header = "",
            IsEnabled = true,
            IsVisible = false,
            Icon = GetIcon(MenuIcons.Copy_Icon)
        };
        modelStatus.Click += async (_, _) => await CopyTextToClipboardAsync(modelStatus.Header);
        serverMenu.Items.Add(modelStatus);
        _serverModelStatusItems[server.Name] = modelStatus;

        var modelSelector = new NativeMenuItem
        {
            Header = MenuText.LoadModel,
            Icon = GetIcon(MenuIcons.CloudDownload_Icon)
        };
        var modelMenu = new NativeMenu();
        modelSelector.Menu = modelMenu;
        serverMenu.Items.Add(modelSelector);
        _serverModelMenus[server.Name] = modelMenu;

        var stopItem = new NativeMenuItem { Icon = GetIcon(MenuIcons.Stop_Icon) };
        stopItem.Click += async (_, _) => await viewModel.StopServerAsync(server);
        serverMenu.Items.Add(stopItem);
        _serverStopItems[server.Name] = stopItem;

        var serverItem = new NativeMenuItem { Menu = serverMenu };
        _topItems[$"{MenuKeys.ServerPrefix}{server.Name}"] = serverItem;
        rootMenu.Items.Add(serverItem);
    }

    private void RefreshModelMenu(ServerViewModel server, NativeMenu modelMenu)
    {
        modelMenu.Items.Clear();
        var compatibleModels = viewModel.Models.Where(server.Supports).ToArray();

        modelMenu.Items.Add(new NativeMenuItem
        {
            Header = MenuText.NoModels,
            IsEnabled = false,
            IsVisible = compatibleModels.Length == 0
        });

        foreach (var model in compatibleModels)
        {
            var item = new NativeMenuItem { Header = model.GetDisplayName() };
            var modelOptionsMenu = new NativeMenu();
            modelOptionsMenu.Items.Add(CreateOptionsMenu(
                $"{MenuText.TemperaturePrefix}: {model.Temperature:0.0}",
                model.Temperature,
                ModelInfo.TemperatureOptions,
                value => viewModel.SetModelTemperature(model, value)));

            if (model.Format == ModelFormat.Gguf)
            {
                modelOptionsMenu.Items.Add(CreateOptionsMenu(
                    $"{MenuText.ReasoningPrefix}: {model.Reasoning}",
                    model.Reasoning,
                    ModelInfo.ReasoningOptions,
                    value => viewModel.SetModelReasoning(model, value)));

                modelOptionsMenu.Items.Add(CreateOptionsMenu(
                    $"{MenuText.ContextSizePrefix}: {FormatUtilities.FormatContextSize(model.ContextSize)}",
                    model.ContextSize,
                    model.ContextSizeOptions.Select(size => (size, $"{FormatUtilities.FormatContextSize(size)} tokens")),
                    value => viewModel.SetModelContextSize(model, value)));
            }

            modelOptionsMenu.Items.Add(CreateAction(
                MenuText.StartModel,
                GetIcon(MenuIcons.Play_Icon),
                async () => await viewModel.StartModelAsync(server, model)));

            item.Menu = modelOptionsMenu;
            modelMenu.Items.Add(item);
        }
    }

    private void RefreshFoundModelsMenu()
    {
        var modelsMenu = _foundModelsMenu!;
        _foundModelsItem!.Header = $"{MenuText.ModelsFound} ({viewModel.Models.Count})";
        modelsMenu.Items.Clear();

        if (viewModel.Models.Count == 0)
        {
            modelsMenu.Items.Add(new NativeMenuItem
            {
                Header = MenuText.NoModels,
                IsEnabled = false
            });
        }
        else
        {
            foreach (var model in viewModel.Models)
            {
                modelsMenu.Items.Add(new NativeMenuItem
                {
                    Header = model.GetDisplayName(),
                    IsEnabled = false
                });
            }
        }

        modelsMenu.Items.Add(new NativeMenuItemSeparator());
        var copyNamesItem = new NativeMenuItem
        {
            Header = MenuText.CopyNames,
            Icon = GetIcon(MenuIcons.Copy_Icon),
            IsEnabled = viewModel.Models.Count > 0
        };
        copyNamesItem.Click += async (_, _) => await CopyTextToClipboardAsync(
            string.Join(Environment.NewLine, viewModel.Models.Select(model => model.GetDisplayName())));
        modelsMenu.Items.Add(copyNamesItem);
    }

    private NativeMenuItem CreateOptionsMenu<T>(
        string header,
        T selectedValue,
        IEnumerable<(T Value, string Label)> options,
        Action<T> selectOption)
    {
        var item = new NativeMenuItem { Header = header, Icon = GetIcon(MenuIcons.Settings_Icon) };
        var menu = new NativeMenu();
        var optionItems = new List<(T Value, NativeMenuItem Item)>();
        foreach (var option in options)
        {
            var value = option.Value;
            var optionItem = CreateAction(
                option.Label,
                GetIcon(MenuIcons.Info_Icon),
                () =>
                {
                    // disable what was just picked and enable every other option
                    selectOption(value);
                    foreach (var (otherValue, otherItem) in optionItems)
                        otherItem.IsEnabled = !EqualityComparer<T>.Default.Equals(otherValue, value);
                    return Task.CompletedTask;
                });
            optionItem.IsEnabled = !EqualityComparer<T>.Default.Equals(value, selectedValue);
            optionItems.Add((value, optionItem));
            menu.Items.Add(optionItem);
        }
        item.Menu = menu;
        return item;
    }

    private async Task CopyTextToClipboardAsync(string text)
    {
        var clipboard = owner.Clipboard;
        if (clipboard is null)
        {
            viewModel.ReportError(
                "Could not copy text to clipboard: clipboard is unavailable.",
                new InvalidOperationException("The window has no clipboard service."));
            return;
        }

        try
        {
            await clipboard.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            viewModel.ReportError("Could not copy text to clipboard.", ex);
        }
    }

    private Avalonia.Media.Imaging.Bitmap GetIcon(string iconName) =>
        MenuIcons.Get(iconName, viewModel.ReportError);

    private static NativeMenuItem CreateAction(string header, Avalonia.Media.Imaging.Bitmap icon, Func<Task> action)
    {
        var item = new NativeMenuItem { Header = header, Icon = icon };
        item.Click += async (_, _) => await action();
        return item;
    }
}
