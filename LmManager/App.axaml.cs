using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using LmManager.Menu;
using LmManager.ViewModels;
using LmManager.Views;

namespace LmManager;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var viewModel = new MainViewModel();
            desktop.Exit += (_, _) => viewModel.Dispose();

            var window = new MainWindow { DataContext = viewModel };
            window.Opened += async (_, _) =>
            {
                window.Hide();
                try
                {
                    await viewModel.InitializeAsync();
                }
                catch (Exception ex)
                {
                    viewModel.ReportError("Initialization failed.", ex);
                }
            };
            desktop.MainWindow = window;

            var menu = new NativeMenu();
            var menuManager = new MenuManager(desktop, window, menu, viewModel);
            menuManager.Build();

            var isMenuOpen = false;
            var normalIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://LmManager/Assets/blue-dot.png")));
            var loadedModelIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://LmManager/Assets/green-dot.png")));
            var errorIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://LmManager/Assets/red-dot.png")));

            menu.NeedsUpdate += (_, _) =>
            {
                if (!isMenuOpen)
                    menuManager.Refresh();
            };
            menu.Opening += async (_, _) =>
            {
                isMenuOpen = true;
                try
                {
                    await viewModel.RefreshAsync();
                }
                catch (Exception ex)
                {
                    viewModel.ReportError("Could not refresh server status.", ex);
                    isMenuOpen = false;
                }
                finally
                {
                    menuManager.Refresh();
                }
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
            trayIcon.Clicked += async (_, _) =>
            {
                try
                {
                    await viewModel.RefreshAsync();
                }
                catch (Exception ex)
                {
                    viewModel.ReportError("Could not refresh server status.", ex);
                }
            };
            viewModel.StateChanged += () =>
                Dispatcher.UIThread.Post(() =>
                {
                    trayIcon.Icon = GetTrayIcon(viewModel, normalIcon, loadedModelIcon, errorIcon);
                    if (!isMenuOpen)
                        menuManager.Refresh();
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
}
