namespace LmManager.Utilities;

public static class FormatUtilities
{
    public static string FormatContextSize(int contextSize) =>
        $"{contextSize / 1000:N0}k";
}
