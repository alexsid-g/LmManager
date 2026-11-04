using LmManager.Models;

namespace LmManager.Services;

public sealed class ModelScanner
{
    public ModelScanResult Scan(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            return new ModelScanResult([], [$"Folder not found: {rootPath}"]);

        var models = new List<ModelInfo>();
        var errors = new List<string>();
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootPath);

        while (pendingDirectories.TryPop(out var directory))
        {
            string[] files;
            string[] childDirectories;
            try
            {
                files = Directory.GetFiles(directory);
                childDirectories = Directory.GetDirectories(directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{directory}: {ex.Message}");
                continue;
            }

            var ggufFiles = files.Where(file => Path.GetExtension(file).Equals(".gguf", StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (var file in ggufFiles)
            {
                try
                {
                    models.Add(new ModelInfo(Path.GetFileNameWithoutExtension(file), file, ModelFormat.Gguf, GetFileSize(file)));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add($"{file}: {ex.Message}");
                }
            }

            if (files.Any(file => Path.GetFileName(file).Equals("config.json", StringComparison.OrdinalIgnoreCase)) &&
                files.Any(file => Path.GetExtension(file).Equals(".safetensors", StringComparison.OrdinalIgnoreCase)))
            {
                var name = new DirectoryInfo(directory).Name;
                models.Add(new ModelInfo(name, directory, ModelFormat.Mlx, GetDirectorySize(directory, errors)));
            }

            foreach (var childDirectory in childDirectories)
            {
                try
                {
                    if ((File.GetAttributes(childDirectory) & FileAttributes.ReparsePoint) == 0)
                        pendingDirectories.Push(childDirectory);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add($"{childDirectory}: {ex.Message}");
                }
            }
        }

        return new ModelScanResult(models.OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase).ToArray(), errors);
    }

    private static long GetFileSize(string path) => new FileInfo(path).Length;

    private static long GetDirectorySize(string path, ICollection<string> errors)
    {
        long size = 0;
        var pending = new Stack<string>();
        pending.Push(path);
        while (pending.TryPop(out var directory))
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    try
                    {
                        size += GetFileSize(file);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        errors.Add($"{file}: {ex.Message}");
                    }
                }

                foreach (var child in Directory.EnumerateDirectories(directory))
                {
                    try
                    {
                        if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0)
                            pending.Push(child);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        errors.Add($"{child}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{directory}: {ex.Message}");
            }
        }
        return size;
    }
}

public sealed record ModelScanResult(IReadOnlyList<ModelInfo> Models, IReadOnlyList<string> Errors);
