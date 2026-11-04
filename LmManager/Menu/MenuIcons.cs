namespace LmManager.Menu;

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Avalonia;
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

    public static readonly StreamGeometry AddRegular = LoadGeometry(Add_Icon);
    public static readonly StreamGeometry AppsRegular = LoadGeometry(Apps_Icon);
    public static readonly StreamGeometry ArrowDownloadRegular = LoadGeometry(ArrowDownload_Icon);
    public static readonly StreamGeometry ArrowRotateClockwiseRegular = LoadGeometry(ArrowRotateClockwise_Icon);
    public static readonly StreamGeometry ArrowSyncRegular = LoadGeometry(ArrowSync_Icon);
    public static readonly StreamGeometry BookmarkRegular = LoadGeometry(Bookmark_Icon);
    public static readonly StreamGeometry BotRegular = LoadGeometry(Bot_Icon);
    public static readonly StreamGeometry CheckmarkCircleRegular = LoadGeometry(CheckmarkCircle_Icon);
    public static readonly StreamGeometry CloudDownloadRegular = LoadGeometry(CloudDownload_Icon);
    public static readonly StreamGeometry CopyRegular = LoadGeometry(Copy_Icon);
    public static readonly StreamGeometry DeleteRegular = LoadGeometry(Delete_Icon);
    public static readonly StreamGeometry DesktopRegular = LoadGeometry(Desktop_Icon);
    public static readonly StreamGeometry DismissRegular = LoadGeometry(Dismiss_Icon);
    public static readonly StreamGeometry DocumentRegular = LoadGeometry(Document_Icon);
    public static readonly StreamGeometry EditRegular = LoadGeometry(Edit_Icon);
    public static readonly StreamGeometry ErrorCircleRegular = LoadGeometry(ErrorCircle_Icon);
    public static readonly StreamGeometry EyeHideRegular = LoadGeometry(EyeHide_Icon);
    public static readonly StreamGeometry EyeShowRegular = LoadGeometry(EyeShow_Icon);
    public static readonly StreamGeometry FolderOpenRegular = LoadGeometry(FolderOpen_Icon);
    public static readonly StreamGeometry FolderRegular = LoadGeometry(Folder_Icon);
    public static readonly StreamGeometry HistoryRegular = LoadGeometry(History_Icon);
    public static readonly StreamGeometry InfoRegular = LoadGeometry(Info_Icon);
    public static readonly StreamGeometry PlayRegular = LoadGeometry(Play_Icon);
    public static readonly StreamGeometry PowerRegular = LoadGeometry(Power_Icon);
    public static readonly StreamGeometry SearchRegular = LoadGeometry(Search_Icon);
    public static readonly StreamGeometry SettingsRegular = LoadGeometry(Settings_Icon);
    public static readonly StreamGeometry StopRegular = LoadGeometry(Stop_Icon);
    public static readonly StreamGeometry WarningRegular = LoadGeometry(Warning_Icon);

    public static Bitmap Get(string iconName)
    {
        var geometry = Icons.GetGeometry(iconName);
        return Icons.Get(geometry);
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


    private static class Icons
    {
        private const int IconSize = 16;
        private const int Scale = 1;
        private const int Dpi = 96;

        private static readonly IBrush DarkBrush =
            new SolidColorBrush(Color.Parse("#444"));

        private static readonly IBrush LightBrush =
            new SolidColorBrush(Color.Parse("#eee"));

        private static readonly Dictionary<string, StreamGeometry> Geometries = new(StringComparer.OrdinalIgnoreCase)
        {
            [Add_Icon] = AddRegular,
            [Apps_Icon] = AppsRegular,
            [ArrowDownload_Icon] = ArrowDownloadRegular,
            [ArrowRotateClockwise_Icon] = ArrowRotateClockwiseRegular,
            [ArrowSync_Icon] = ArrowSyncRegular,
            [Bookmark_Icon] = BookmarkRegular,
            [Bot_Icon] = BotRegular,
            [CheckmarkCircle_Icon] = CheckmarkCircleRegular,
            [CloudDownload_Icon] = CloudDownloadRegular,
            [Copy_Icon] = CopyRegular,
            [Delete_Icon] = DeleteRegular,
            [Desktop_Icon] = DesktopRegular,
            [Dismiss_Icon] = DismissRegular,
            [Document_Icon] = DocumentRegular,
            [Edit_Icon] = EditRegular,
            [ErrorCircle_Icon] = ErrorCircleRegular,
            [EyeHide_Icon] = EyeHideRegular,
            [EyeShow_Icon] = EyeShowRegular,
            [FolderOpen_Icon] = FolderOpenRegular,
            [Folder_Icon] = FolderRegular,
            [History_Icon] = HistoryRegular,
            [Info_Icon] = InfoRegular,
            [Play_Icon] = PlayRegular,
            [Power_Icon] = PowerRegular,
            [Search_Icon] = SearchRegular,
            [Settings_Icon] = SettingsRegular,
            [Stop_Icon] = StopRegular,
            [Warning_Icon] = WarningRegular
        };

        public static StreamGeometry GetGeometry(string iconName)
        {
            if (!Geometries.TryGetValue(iconName, out var geometry))
                throw new ArgumentException($"Icon '{iconName}' not found.", nameof(iconName));

            return geometry;
        }

        public static Bitmap Get(StreamGeometry geometry)
        {
            if (Cache.TryGetValue(geometry, out var bitmap))
                return bitmap;

            var renderTarget = new RenderTargetBitmap(
                new PixelSize(IconSize * Scale, IconSize * Scale),
                new Vector(Dpi * Scale, Dpi * Scale));

            using (var context = renderTarget.CreateDrawingContext())
            using (context.PushTransform(Matrix.CreateScale(IconSize * Scale, IconSize * Scale)))
            {
                bool isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
                context.DrawGeometry(Icons.GetIconBrush(!isDark), null, geometry);
            }

            Cache[geometry] = renderTarget;
            return renderTarget;
        }

        public static IBrush GetIconBrush(bool isDark) =>
            isDark? DarkBrush : LightBrush;
    }

}
