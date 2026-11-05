namespace LmManager.Menu;

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;

/// <summary>
/// SVG icons loaded from the application's Assets and rendered as cached bitmaps.
/// </summary>
public static class MenuIcons
{
    private static readonly Dictionary<StreamGeometry, Bitmap> Cache = new();

    // Error fallback geometry in case an icon fails to load or render.
    // This is a simple red circle with an exclamation mark.
    private const string ErrorFallback = "M0.5,0.08333 C0.73013,0.08333 0.91667,0.26992 0.91667,0.5 C0.91667,0.73008 0.73013,0.91667 0.5,0.91667 C0.26988,0.91667 0.08333,0.73008 0.08333,0.5 C0.08333,0.26992 0.26988,0.08333 0.5,0.08333 Z M0.5,0.15279 C0.30854,0.15279 0.15279,0.30854 0.15279,0.5 C0.15279,0.69146 0.30854,0.84721 0.5,0.84721 C0.69146,0.84721 0.84721,0.69146 0.84721,0.5 C0.84721,0.30854 0.69146,0.15279 0.5,0.15279 Z M0.49994,0.60426 C0.52293,0.60426 0.54156,0.62289 0.54156,0.64587 C0.54156,0.66885 0.52293,0.68748 0.49994,0.68748 C0.47696,0.68748 0.45833,0.66885 0.45833,0.64587 C0.45833,0.62289 0.47696,0.60426 0.49994,0.60426 Z M0.49977,0.29167 C0.51559,0.29165 0.52867,0.3034 0.53076,0.31865 L0.53104,0.32289 L0.53119,0.51046 C0.53121,0.52772 0.51723,0.54172 0.49997,0.54173 C0.48415,0.54175 0.47106,0.53 0.46898,0.51475 L0.46869,0.51051 L0.46854,0.32294 C0.46853,0.30568 0.48251,0.29168 0.49977,0.29167 Z";
    private static readonly Lazy<Bitmap> ErrorFallbackBitmap = new(() =>
    {
        StreamGeometry geometry = StreamGeometry.Parse(ErrorFallback);
        return Icons.GetBitmap(geometry, isRed: true);
    });

    public const string Add_Icon = "add_regular";
    public const string Apps_Icon = "apps_regular";
    public const string ArrowDownload_Icon = "arrow_download_regular";
    public const string ArrowRotateClockwise_Icon = "arrow_rotate_clockwise_regular";
    public const string ArrowSync_Icon = "arrow_sync_regular";
    public const string Bookmark_Icon = "bookmark_regular";
    public const string Bot_Icon = "bot_regular";
    public const string CheckmarkCircle_Icon = "checkmark_circle_regular";
    public const string CloudDownload_Icon = "cloud_download_regular";
    public const string Copy_Icon = "copy_regular";
    public const string Delete_Icon = "delete_regular";
    public const string Desktop_Icon = "desktop_regular";
    public const string Dismiss_Icon = "dismiss_regular";
    public const string Document_Icon = "document_regular";
    public const string Edit_Icon = "edit_regular";
    public const string ErrorCircle_Icon = "error_circle_regular";
    public const string EyeHide_Icon = "eye_hide_regular";
    public const string EyeShow_Icon = "eye_show_regular";
    public const string FolderOpen_Icon = "folder_open_regular";
    public const string Folder_Icon = "folder_regular";
    public const string History_Icon = "history_regular";
    public const string Info_Icon = "info_regular";
    public const string Play_Icon = "play_regular";
    public const string Power_Icon = "power_regular";
    public const string Search_Icon = "search_regular";
    public const string Settings_Icon = "settings_regular";
    public const string Stop_Icon = "stop_regular";
    public const string Warning_Icon = "warning_regular";

    static MenuIcons()
    {
        Application.Current?.ActualThemeVariantChanged += (_, _) =>
        {
            Cache.Clear();
        };
    }

    public static Bitmap Get(string iconName, Action<string, Exception> reportError)
    {
        try
        {    
            var geometry = Icons.GetGeometry(iconName);
            return Icons.GetBitmap(geometry);
        }
        catch (Exception ex)
        {
            reportError($"Error loading icon '{iconName}'.", ex);
            return ErrorFallbackBitmap.Value;
        }
    }

    private static class Icons
    {
        private const int IconSize = 16;
        private const int Scale = 1;
        private const int Dpi = 96;

        private static readonly IBrush DarkBrush =
            new SolidColorBrush(Color.Parse("#222"));

        private static readonly IBrush LightBrush =
            new SolidColorBrush(Color.Parse("#eee"));

        private static readonly Dictionary<string, StreamGeometry> Geometries = new(StringComparer.OrdinalIgnoreCase);

        public static StreamGeometry GetGeometry(string iconName)
        {
            if (Geometries.TryGetValue(iconName, out var geometry))
                return geometry;

            geometry = LoadGeometry(iconName);
            Geometries[iconName] = geometry;
            return geometry;
        }

        public static Bitmap GetBitmap(StreamGeometry geometry, bool isRed = false)
        {
            if (Cache.TryGetValue(geometry, out var bitmap))
                return bitmap;

            double scale = Scale;
            if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null)
                {
                    scale = desktop.MainWindow.RenderScaling;
                }
            }

            var pixelSize = (int)Math.Ceiling(IconSize * scale);
            var renderTarget = new RenderTargetBitmap(
                new PixelSize(pixelSize, pixelSize),
                new Vector(Dpi * scale, Dpi * scale));

            using (var context = renderTarget.CreateDrawingContext())
            using (context.PushTransform(Matrix.CreateScale(IconSize, IconSize)))
            {
                var useLightOutline = Application.Current?.ActualThemeVariant != ThemeVariant.Dark;
                IBrush brush = isRed ? Brushes.Red : GetIconBrush(useLightOutline);
                context.DrawGeometry(brush, null, geometry);
            }

            Cache[geometry] = renderTarget;
            return renderTarget;
        }

        private static StreamGeometry LoadGeometry(string assetName)
        {
            var assetUri = new Uri($"avares://LmManager/Assets/Svg/{assetName}.svg");
            using var stream = AssetLoader.Open(assetUri);

            var document = XDocument.Load(stream);
            XNamespace svgNamespace = "http://www.w3.org/2000/svg";
            var pathData = document
                .Descendants(svgNamespace + "path")
                .Select(path => (string?)path.Attribute("d"))
                .Where(data => !string.IsNullOrWhiteSpace(data))
                .ToArray();

            if (pathData.Length == 0)
                throw new InvalidDataException($"SVG asset '{assetName}' contains no path data.");

            return StreamGeometry.Parse(string.Join(" ", pathData));
        }

        private static IBrush GetIconBrush(bool isDarkBrush) =>
            isDarkBrush ? DarkBrush : LightBrush;
    }

}
