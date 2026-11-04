using System.Text.Json;
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

            models.AddRange(FindGgufModels(files, errors));

            var mlxModel = FindMlxModel(directory, files, errors);
            if (mlxModel is not null)
                models.Add(mlxModel);

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

    private static IEnumerable<ModelInfo> FindGgufModels(string[] files, ICollection<string> errors)
    {
        var validGgufFiles = new List<string>();
        foreach (var file in files.Where(file =>
                     Path.GetExtension(file).Equals(".gguf", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                if (IsGguf(file))
                    validGgufFiles.Add(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{file}: {ex.Message}");
            }
        }

        var mmprojFileName = validGgufFiles
            .Where(IsMmprojFile)
            .Select(Path.GetFileName)
            .OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        foreach (var file in validGgufFiles.Where(file => !IsMmprojFile(file)))
        {
            ModelInfo? model = null;
            try
            {
                model = new ModelInfo(
                    Path.GetFileNameWithoutExtension(file),
                    file,
                    ModelFormat.Gguf,
                    GetFileSize(file),
                    mmprojFileName);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{file}: {ex.Message}");
            }

            if (model is not null)
                yield return model;
        }
    }

    private static bool IsMmprojFile(string filePath) =>
        Path.GetFileName(filePath).StartsWith("mmproj-", StringComparison.OrdinalIgnoreCase);

    private static ModelInfo? FindMlxModel(string directory, string[] files, ICollection<string> errors)
    {
        if (!files.Any(file => Path.GetFileName(file)
            .Equals("config.json", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var hasMlxSafeTensors = false;
        foreach (var file in files.Where(file => Path.GetExtension(file)
            .Equals(".safetensors", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                if (IsMlxSafeTensors(file))
                {
                    hasMlxSafeTensors = true;
                    break;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                errors.Add($"{file}: {ex.Message}");
            }
        }

        if (!hasMlxSafeTensors)
            return null;

        var name = new DirectoryInfo(directory).Name;
        return new ModelInfo(name, directory, ModelFormat.Mlx, GetDirectorySize(directory, errors));
    }

    private static bool IsGguf(string filePath)
    {
        Span<byte> magic = stackalloc byte[4];

        using var stream = File.OpenRead(filePath);
        return stream.Read(magic) == 4
            && magic.SequenceEqual("GGUF"u8);
    }

    private static bool IsMlxSafeTensors(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        if (stream.Length < 8)
            return false;

        Span<byte> lengthBuffer = stackalloc byte[8];
        stream.ReadExactly(lengthBuffer);

        var headerLength = (long)BitConverter.ToUInt64(lengthBuffer);
        if (headerLength > stream.Length - 8 || headerLength > 1024 * 1024)
            return false;

        var header = new byte[headerLength];
        stream.ReadExactly(header);

        using var document = JsonDocument.Parse(header);
        return document.RootElement.TryGetProperty("__metadata__", out var metadata)
            && metadata.TryGetProperty("format", out var format)
            && format.ValueKind == JsonValueKind.String
            && format.GetString() == "mlx";
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
