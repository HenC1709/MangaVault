using MangaVault.Core.Entities;
using MangaVault.Infrastructure.Logging;
using MangaVault.Infrastructure.Persistence.Sqlite;

namespace MangaVault.Presentation.Cli.Menus;

public sealed class MainMenu(
    UserMenu userMenu,
    MangaMenu mangaMenu,
    string dataDirectory,
    string databasePath,
    JsonToSqliteImporter importer,
    ConsoleLogger logger)
{
    private Session? _currentSession;

    public void Run()
    {
        bool running = true;
        while (running)
        {
            Console.WriteLine();
            Console.WriteLine("=== MangaVault ===");
            Console.WriteLine(_currentSession is null
                ? "Usuario actual: Invitado"
                : $"Usuario actual: {_currentSession.UserName} ({_currentSession.Role})");
            Console.WriteLine("1. Catálogo y biblioteca (Mangas / Novelas Ligeras)");
            Console.WriteLine("2. Cuenta y sesión");
            Console.WriteLine("3. Ver carpeta de datos y BD");
            Console.WriteLine("4. Importar datos desde JSON (migración)");
            Console.WriteLine("0. Salir");
            Console.Write("Opción: ");

            string? option = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (option)
            {
                case "1":
                    mangaMenu.Show(_currentSession);
                    break;
                case "2":
                    _currentSession = userMenu.Show(_currentSession);
                    break;
                case "3":
                    Console.WriteLine($"Carpeta de datos: {dataDirectory}");
                    Console.WriteLine($"Base de datos SQLite: {databasePath}");
                    break;
                case "4":
                    ImportJsonData();
                    break;
                case "0":
                    running = false;
                    Console.WriteLine("Hasta luego.");
                    break;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }
    }

    private void ImportJsonData()
    {
        if (!JsonToSqliteImporter.HasJsonDataFiles(dataDirectory))
        {
            Console.WriteLine($"No se encontraron archivos JSON heredados (users.json, mangas.json, favorites.json) en '{dataDirectory}'.");
            return;
        }

        JsonImportResult result = importer.Import(dataDirectory);
        logger.Info(
            $"Importación JSON -> SQLite completada: usuarios ({result.UsersImported}/{result.UsersSkipped}), obras ({result.WorksImported}/{result.WorksSkipped}), biblioteca ({result.EntriesImported}/{result.EntriesSkipped}).");

        Console.WriteLine("Migración desde JSON completada:");
        Console.WriteLine($"- Usuarios: {result.UsersImported} importados, {result.UsersSkipped} omitidos");
        Console.WriteLine($"- Obras: {result.WorksImported} importadas, {result.WorksSkipped} omitidas");
        Console.WriteLine($"- Registros de biblioteca: {result.EntriesImported} importados, {result.EntriesSkipped} omitidos");
    }
}
