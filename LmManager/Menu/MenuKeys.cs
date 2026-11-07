namespace LmManager.Menu;

/// <summary>
/// Registry keys for the persisted top-level menu items. Centralised so the
/// dictionaries are keyed identically between the build (CreateTopLevelItems)
/// and refresh (RefreshMenu) phases.
/// </summary>
public static class MenuKeys
{
    public const string Title = "title";
    public const string Models = "models";
    public const string CurrentStatus = "currentStatus";
    public const string ShowLog = "showLog";
    public const string Quit = "quit";

    // Server items are stored per server, prefixed so they never collide
    // with the static keys above.
    public const string ServerPrefix = "server:";
}
