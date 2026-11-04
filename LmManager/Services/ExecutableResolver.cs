namespace LmManager.Services;

public static class ExecutableResolver
{
    public static string? Find(string? configuredPath, string executableName)
    {
        var configuredFileName = string.IsNullOrWhiteSpace(configuredPath)
            ? executableName
            : Path.GetFileName(configuredPath);

        var directCandidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configuredPath) &&
            (Path.IsPathRooted(configuredPath) || Path.GetDirectoryName(configuredPath) is not null))
        {
            directCandidates.Add(configuredPath);
        }

        var directories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        if (OperatingSystem.IsMacOS())
            directories.InsertRange(0, ["/opt/homebrew/bin", "/usr/local/bin"]);

        var executableNames = new[] { configuredFileName, executableName }
            .Distinct(StringComparer.Ordinal)
            .SelectMany(GetExecutableNames);
        directCandidates.AddRange(
            from directory in directories
            from name in executableNames
            select Path.Combine(directory, name));

        return directCandidates
            .Distinct(StringComparer.Ordinal)
            .FirstOrDefault(IsExecutableFile);
    }

    private static bool IsExecutableFile(string path)
    {
        if (!File.Exists(path))
            return false;

        if (OperatingSystem.IsWindows())
            return true;

        var executablePermissions =
            UnixFileMode.UserExecute |
            UnixFileMode.GroupExecute |
            UnixFileMode.OtherExecute;
        return (File.GetUnixFileMode(path) & executablePermissions) != 0;
    }

    private static IEnumerable<string> GetExecutableNames(string name)
    {
        yield return name;
        if (!OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Path.GetExtension(name)))
            yield break;

        var extensions = Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM";
        foreach (var extension in extensions.Split(';', StringSplitOptions.RemoveEmptyEntries))
            yield return name + extension;
    }
}
