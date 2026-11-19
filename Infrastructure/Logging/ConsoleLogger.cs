namespace MangaVault.Infrastructure.Logging;

public sealed class ConsoleLogger
{
    public void Info(string message) =>
        Write("INFO", message, ConsoleColor.Cyan);

    public void Warning(string message) =>
        Write("WARN", message, ConsoleColor.Yellow);

    public void Error(string message, Exception? ex = null)
    {
        string detail = ex is null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}";
        Write("ERROR", detail, ConsoleColor.Red);
    }

    private static void Write(string level, string message, ConsoleColor color)
    {
        ConsoleColor previous = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }
}
