using System.IO;
using MangaVault.Infrastructure.Persistence.Sqlite;

namespace MangaVault.Wpf.Services;

/// <summary>
/// Rutas de datos de la aplicación de escritorio. Usa la misma convención que la CLI
/// (<c>%LOCALAPPDATA%\MangaVault</c> o la variable <c>MANGAVAULT_DATA_DIR</c>) para compartir la misma BD.
/// </summary>
public sealed record AppEnvironment(string DataDirectory, string DatabasePath, string LogsDirectory)
{
    public const string DataDirectoryVariable = "MANGAVAULT_DATA_DIR";

    public static AppEnvironment ForDataDirectory(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

        string fullPath = Path.GetFullPath(dataDirectory.Trim());
        return new AppEnvironment(
            fullPath,
            SqliteDatabase.GetDefaultDatabasePath(fullPath),
            Path.Combine(fullPath, "logs"));
    }

    public static AppEnvironment FromEnvironment()
    {
        string? custom = Environment.GetEnvironmentVariable(DataDirectoryVariable);
        string dataDirectory = !string.IsNullOrWhiteSpace(custom)
            ? custom
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MangaVault");

        return ForDataDirectory(dataDirectory);
    }
}
