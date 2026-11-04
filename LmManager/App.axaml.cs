using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using LmManager.Menu;
using LmManager.ViewModels;
using LmManager.Views;

namespace LmManager;

public partial class App : Application
{
    // Persistent top-level chrome, built once and reused across refreshes.
    private readonly Dictionary<string, NativeMenuItem> _topItems = new();
    private readonly Dictionary<string, NativeMenu> _serverModelMenus = new();
    private readonly Dictionary<string, NativeMenuItem> _serverModelStatusItems = new();
    private readonly Dictionary<string, NativeMenuItem> _serverStopItems = new();
    private bool _menuBuilt;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var viewModel = new MainViewModel();
            desktop.Exit += (_, _) => viewModel.Dispose();

            var window = new MainWindow { DataContext = viewModel };
            window.Opened += (_, _) =>
            {
                var initialization = viewModel.InitializeAsync();
                window.Hide();
                _ = initialization;
            };
            desktop.MainWindow = window;

            var menu = new NativeMenu();
            var isMenuOpen = false;
            var normalIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://LmManager/Assets/blue-dot.png")));
            var loadedModelIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://LmManager/Assets/green-dot.png")));
            var errorIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://LmManager/Assets/red-dot.png")));

            UpdateMenu(menu, window, viewModel, desktop);
            menu.NeedsUpdate += (_, _) =>
            {
                if (!isMenuOpen)
                    UpdateMenu(menu, window, viewModel, desktop);
            };
            menu.Opening += async (_, _) =>
            {
                isMenuOpen = true;
                await viewModel.RefreshAsync();
                UpdateMenu(menu, window, viewModel, desktop);
            };
            menu.Closed += (_, _) =>
            {
                isMenuOpen = false;
            };

            var trayIcon = new TrayIcon
            {
                Icon = GetTrayIcon(viewModel, normalIcon, loadedModelIcon, errorIcon),
                ToolTipText = MenuText.TitlePrefix,
                IsVisible = true,
                Menu = menu
            };
            trayIcon.Clicked += async (_, _) => await viewModel.RefreshAsync();
            viewModel.StateChanged += () =>
                Dispatcher.UIThread.Post(() =>
                {
                    trayIcon.Icon = GetTrayIcon(viewModel, normalIcon, loadedModelIcon, errorIcon);
                    if (!isMenuOpen)
                        UpdateMenu(menu, window, viewModel, desktop);
                });

            TrayIcon.SetIcons(this, new TrayIcons { trayIcon });
            window.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static WindowIcon GetTrayIcon(
        MainViewModel viewModel,
        WindowIcon normalIcon,
        WindowIcon loadedModelIcon,
        WindowIcon errorIcon) =>
        viewModel.HasError
            ? errorIcon
            : viewModel.HasLoadedModel
                ? loadedModelIcon
                : normalIcon;

    private void UpdateMenu(
        NativeMenu menu,
        Window owner,
        MainViewModel viewModel,
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (!_menuBuilt)
        {
            CreateTopLevelItems(menu, owner, viewModel, desktop);
            _menuBuilt = true;
        }

        RefreshMenu(viewModel);
    }

    // Build the persistent chrome once and register every top-level item in a
    // dictionary, so later updates only need to change text / rebuild leaf content.
    private void CreateTopLevelItems(
        NativeMenu menu,
        Window owner,
        MainViewModel viewModel,
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        menu.Items.Clear();

        var title = new NativeMenuItem
        {
            Header = "",
            IsEnabled = true,
            Icon = MenuIcons.Get(MenuIcons.Copy_Icon),
        };
        title.Click += (_, _) => CopyTextToClipboard(owner, title.Header);
        _topItems[MenuKeys.Title] = title;

        var modelsItem = new NativeMenuItem { Header = "", Icon = MenuIcons.Get(MenuIcons.Folder_Icon) };
        var modelsMenu = new NativeMenu();
        modelsMenu.Items.Add(CreateAction(MenuText.ChooseFolder, MenuIcons.Get(MenuIcons.FolderOpen_Icon), async () => await viewModel.ChooseModelsDirectoryAsync(owner)));
        modelsMenu.Items.Add(CreateAction(MenuText.ScanNow, MenuIcons.Get(MenuIcons.Search_Icon), async () => await viewModel.ScanModelsAsync()));
        modelsItem.Menu = modelsMenu;
        _topItems[MenuKeys.Models] = modelsItem;

        menu.Items.Add(title);
        menu.Items.Add(modelsItem);
        menu.Items.Add(new NativeMenuItemSeparator());

        foreach (var server in viewModel.Servers)
        {
            var serverMenu = new NativeMenu();
            var modelStatus = new NativeMenuItem 
            {
                Header = "", 
                IsEnabled = true,
                IsVisible = false,
                Icon = MenuIcons.Get(MenuIcons.Copy_Icon)
            };
            modelStatus.Click += (_, _) => CopyTextToClipboard(owner, modelStatus.Header);
            serverMenu.Items.Add(modelStatus);
            _serverModelStatusItems[server.Name] = modelStatus;

            var modelSelector = new NativeMenuItem { Header = MenuText.LoadModel, Icon = MenuIcons.Get(MenuIcons.CloudDownload_Icon) };
            var modelMenu = new NativeMenu();
            modelSelector.Menu = modelMenu;
            serverMenu.Items.Add(modelSelector);
            _serverModelMenus[server.Name] = modelMenu;

            var stopItem = new NativeMenuItem { Icon = MenuIcons.Get(MenuIcons.Stop_Icon) };
            stopItem.Click += async (_, _) => await viewModel.StopServerAsync(server);
            serverMenu.Items.Add(stopItem);
            _serverStopItems[server.Name] = stopItem;

            var serverItem = new NativeMenuItem { Menu = serverMenu };
            _topItems[$"{MenuKeys.ServerPrefix}{server.Name}"] = serverItem;

            menu.Items.Add(serverItem);
        }

        menu.Items.Add(new NativeMenuItemSeparator());

        var showLog = new NativeMenuItem { Header = MenuText.ShowLog, Icon = MenuIcons.Get(MenuIcons.Document_Icon) };
        showLog.Click += (_, _) => viewModel.OpenLogFolder();
        _topItems[MenuKeys.ShowLog] = showLog;

        var exitItem = new NativeMenuItem { Header = MenuText.Quit, Icon = MenuIcons.Get(MenuIcons.Dismiss_Icon) };
        exitItem.Click += (_, _) => desktop.Shutdown();
        _topItems[MenuKeys.Quit] = exitItem;

        menu.Items.Add(showLog);
        menu.Items.Add(exitItem);
    }

    // Runs on every open / state change. Only text and the dynamic model list
    // change here; the registered top-level items are reused.
    private void RefreshMenu(MainViewModel viewModel)
    {
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

            RefreshModelMenu(server, _serverModelMenus[server.Name], viewModel);
        }
    }

    private static void RefreshModelMenu(ServerViewModel server, NativeMenu modelMenu, MainViewModel viewModel)
    {
        modelMenu.Items.Clear();
        var compatibleModels = viewModel.Models.Where(server.Supports).ToArray();

        var emptyItem = new NativeMenuItem
        {
            Header = MenuText.NoModels,
            IsEnabled = false,
            IsVisible = compatibleModels.Length == 0
        };
        modelMenu.Items.Add(emptyItem);

        foreach (var model in compatibleModels)
        {
            var item = CreateAction(model.DisplayName, MenuIcons.Get(MenuIcons.Play_Icon), async () => await viewModel.StartModelAsync(server, model));
            item.ToolTip = model.Path;
            modelMenu.Items.Add(item);
        }
    }

    private static void CopyTextToClipboard(TopLevel topLevel, string text)
    {
        try
        {
            topLevel.Clipboard?.SetTextAsync(text);
        }
        catch
        {
            // clipboard unavailable - ignore
        }
    }

    private static NativeMenuItem CreateAction(string header, Avalonia.Media.Imaging.Bitmap icon, Func<Task> action)
    {
        var item = new NativeMenuItem { Header = header, Icon = icon };
        item.Click += async (_, _) => await action();
        return item;
    }
}
