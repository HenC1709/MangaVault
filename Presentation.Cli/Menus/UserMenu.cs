using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Entities;
using MangaVault.Core.Exceptions;
using MangaVault.Infrastructure.Logging;

namespace MangaVault.Presentation.Cli.Menus;

public sealed class UserMenu(UserService userService, ConsoleLogger logger)
{
    public Session? Show(Session? currentSession)
    {
        bool back = false;
        while (!back)
        {
            Console.WriteLine();
            Console.WriteLine("=== Cuenta y Sesión ===");
            Console.WriteLine(currentSession is null
                ? "Estado: Sin sesión iniciada"
                : $"Sesión activa: {currentSession.UserName} ({currentSession.Role})");
            Console.WriteLine("1. Registrar usuario");
            Console.WriteLine("2. Iniciar sesión");
            if (currentSession is not null)
            {
                Console.WriteLine("3. Cambiar contraseña");
                Console.WriteLine("4. Cerrar sesión");
            }
            Console.WriteLine("0. Volver");
            Console.Write("Opción: ");

            string? option = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (option)
            {
                case "1":
                    Register();
                    break;
                case "2":
                    currentSession = Login() ?? currentSession;
                    break;
                case "3" when currentSession is not null:
                    ChangePassword(currentSession);
                    break;
                case "4" when currentSession is not null:
                    logger.Info($"Sesión cerrada para '{currentSession.UserName}'.");
                    currentSession = null;
                    Console.WriteLine("Sesión cerrada.");
                    break;
                case "0":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }

        return currentSession;
    }

    private void Register()
    {
        Console.Write("Nombre de usuario: ");
        string name = Console.ReadLine() ?? "";

        Console.Write("Contraseña: ");
        string password = Console.ReadLine() ?? "";

        try
        {
            User created = userService.Register(new CreateUserDto(name, password));
            logger.Info($"Usuario registrado: {created.Name} (Id {created.UserId}).");
            Console.WriteLine($"Usuario '{created.Name}' registrado con Id {created.UserId}.");
        }
        catch (DuplicateUserException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Dato inválido: {ex.Message}");
        }
    }

    private Session? Login()
    {
        Console.Write("Nombre de usuario: ");
        string name = Console.ReadLine() ?? "";

        Console.Write("Contraseña: ");
        string password = Console.ReadLine() ?? "";

        try
        {
            Session session = userService.Authenticate(name, password);
            logger.Info($"Inicio de sesión exitoso: {session.UserName}.");
            Console.WriteLine($"Bienvenido, {session.UserName}.");
            return session;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.Warning($"Intento de inicio de sesión fallido para '{name.Trim()}'.");
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    private void ChangePassword(Session session)
    {
        Console.Write("Contraseña actual: ");
        string currentPassword = Console.ReadLine() ?? "";

        Console.Write("Nueva contraseña: ");
        string newPassword = Console.ReadLine() ?? "";

        try
        {
            userService.ChangePassword(session.UserId, currentPassword, newPassword);
            logger.Info($"Contraseña actualizada para '{session.UserName}'.");
            Console.WriteLine("Contraseña actualizada correctamente.");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or UserNotFoundException or ArgumentException)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
