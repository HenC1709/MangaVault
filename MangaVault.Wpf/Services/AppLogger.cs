using System.Globalization;
using System.IO;

namespace MangaVault.Wpf.Services;

public interface IAppLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
}

/// <summary>
/// Registro en archivo para la app de escritorio (no hay consola en una WinExe).
/// Escribe en <c>{DataDirectory}\logs\mangavault-wpf-yyyyMMdd.log</c>.
/// </summary>
public sealed class FileAppLogger(string logsDirectory) : IAppLogger
{
    private readonly Lock _sync = new();
    private readonly string _logsDirectory = string.IsNullOrWhiteSpace(logsDirectory)
        ? throw new ArgumentException("La carpeta de logs es obligatoria.", nameof(logsDirectory))
        : logsDirectory;

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message}{Environment.NewLine}{exception}");

    private void Write(string level, string message)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        string line = $"[{timestamp} UTC] [{level}] {message}{Environment.NewLine}";
        string path = Path.Combine(
            _logsDirectory,
            $"mangavault-wpf-{DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.log");

        lock (_sync)
        {
            try
            {
                Directory.CreateDirectory(_logsDirectory);
                File.AppendAllText(path, line);
            }
            catch (IOException)
            {
                // El registro nunca debe derribar la aplicación.
            }
            catch (UnauthorizedAccessException)
            {
                // Idem.
            }
        }
    }
}
