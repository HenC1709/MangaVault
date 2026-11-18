using System.Text.Json;

namespace MangaVault.Infrastructure.Persistence.Json;

internal sealed class JsonFileStore<T>(string fileName, string? storageDirectory = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly Lock _sync = new();
    private readonly string _path = Path.Combine(
        storageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MangaVault"),
        fileName);

    public List<T> ReadAll()
    {
        lock (_sync)
        {
            return ReadAllUnlocked();
        }
    }

    public TResult Read<TResult>(Func<List<T>, TResult> query)
    {
        lock (_sync)
        {
            return query(ReadAllUnlocked());
        }
    }

    public void Mutate(Action<List<T>> update)
    {
        lock (_sync)
        {
            var items = ReadAllUnlocked();
            update(items);
            WriteAllAtomicUnlocked(items);
        }
    }

    private List<T> ReadAllUnlocked()
    {
        if (!File.Exists(_path))
            return [];

        string json = File.ReadAllText(_path);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<T>>(json) ?? [];
    }

    private void WriteAllAtomicUnlocked(List<T> items)
    {
        string dir = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(dir);

        string tempPath = Path.Combine(dir, $"{Path.GetFileName(_path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, items, JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, _path, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Ignorar error de limpieza de temporal para preservar la excepción original.
            }

            throw;
        }
    }
}
