using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Controls.ApplicationLifetimes;
using LmManager.Models;
using LmManager.ViewModels;

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

        _topItems[MenuKeys.Title].Header = $"{MenuText.TitlePrefix}{MenuText.StatusSeparator}{viewModel.StatusMessage}";
        _topItems[MenuKeys.Models].Header = $"{MenuText.ModelsFolderPrefix}: {viewModel.ModelsDirectory}";

        foreach (var server in viewModel.Servers)
        {
            _topItems[$"{MenuKeys.ServerPrefix}{server.Name}"].Header =
                $"{server.Name}{MenuText.StatusSeparator}{(server.IsRunning ? MenuText.Running : MenuText.Stopped)} ({server.Address})";

            var modelStatus = _serverModelStatusItems[server.Name];
            modelStatus.IsVisible = !string.IsNullOrWhiteSpace(server.CurrentModel);
            modelStatus.Header = $"{MenuText.ModelLabel}: {server.CurrentModel}";

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
        modelsItem.Menu = modelsMenu;
        _topItems[MenuKeys.Models] = modelsItem;

        rootMenu.Items.Add(title);
        rootMenu.Items.Add(modelsItem);
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
        CreateTopLevelItems();
        Refresh();
    }

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
            var item = new NativeMenuItem { Header = model.DisplayName };
            var modelOptionsMenu = new NativeMenu();
            modelOptionsMenu.Items.Add(CreateOptionsMenu(
                $"{MenuText.TemperaturePrefix}: {model.Temperature:0.0}",
                model.Temperature,
                ModelInfo.TemperatureOptions,
                value => viewModel.SetModelTemperature(model, value)));

            if (model.Format == ModelFormat.Gguf)
                modelOptionsMenu.Items.Add(CreateOptionsMenu(
                    $"{MenuText.ContextSizePrefix}: {model.ContextSize / 1000:N0}k",
                    model.ContextSize,
                    model.ContextSizeOptions.Select(size => (size, $"{size / 1000:N0}k tokens")),
                    value => viewModel.SetModelContextSize(model, value)));

            modelOptionsMenu.Items.Add(CreateAction(
                MenuText.StartModel,
                GetIcon(MenuIcons.Play_Icon),
                async () => await viewModel.StartModelAsync(server, model)));

            item.Menu = modelOptionsMenu;
            item.ToolTip = model.Path;
            modelMenu.Items.Add(item);
        }
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
