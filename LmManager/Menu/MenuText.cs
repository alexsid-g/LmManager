namespace LmManager.Menu;

/// <summary>
/// Single source of truth for the tray-menu text. Every visible menu item
/// label is a constant here so the menu wording is defined once and reused.
/// Dynamic lines (status, paths, model names) are composed from these constants.
/// </summary>
public static class MenuText
{
    public const string TitlePrefix = "Local LLM Manager";
    public const string StatusSeparator = " - ";

    public const string ModelsFolderPrefix = "Models folder";
    public const string CurrentStatus = "Current Status (copy)";
    public const string ServerAddressLabel = "Server address";
    public const string ModelParametersLabel = "Model params";
    public const string StatusMessageLabel = "Status message";
    public const string OpenServer = "Open server (LLAMA.CPP)";
    public const string OpenModel = "Open model (LLAMA.CPP)";
    public const string OpenParams = "Open params (LLAMA.CPP)";
    public const string ValueUnavailable = "Not available";
    public const string ChooseFolder = "Choose folder...";
    public const string ScanNow = "Scan now";
    public const string ModelsFound = "Found Models List";
    public const string CopyNames = "Copy names";
    public const string LoadModel = "Load model";
    public const string NoModels = "Empty";
    public const string ContextSizePrefix = "Context size";
    public const string TemperaturePrefix = "Temperature";
    public const string ReasoningPrefix = "Reasoning";
    public const string StartModel = "Load this model";

    public const string ModelLabel = "Model";
    public const string Running = "Running";
    public const string Stopped = "Stopped";

    public const string StopServer = "Stop server";
    public const string StopExternalServer = "Stop external server";
    public const string StopExternalServerOnly = "Stop external server (macOS only)";

    public const string ShowLog = "Show Log";
    public const string Quit = "Quit";
}
