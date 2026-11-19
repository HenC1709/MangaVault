using MangaVault.Core.Entities;

namespace MangaVault.Presentation.Cli.Menus;

public sealed class MainMenu(UserMenu userMenu, MangaMenu mangaMenu, string dataDirectory)
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
            Console.WriteLine("3. Ver carpeta de datos");
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
}
