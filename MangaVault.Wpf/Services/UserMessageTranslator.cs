using MangaVault.Core.Constants;
using MangaVault.Core.Exceptions;
using Microsoft.Data.Sqlite;

namespace MangaVault.Wpf.Services;

/// <summary>
/// Convierte excepciones en mensajes comprensibles. Los mensajes de dominio/Application ya están
/// en español; los errores técnicos (SQLite, inesperados) nunca se muestran tal cual.
/// </summary>
public static class UserMessageTranslator
{
    public const string UnexpectedError =
        "Ocurrió un error inesperado. Los detalles técnicos se guardaron en el registro de la aplicación.";

    public const string DataAccessError =
        "No se pudo acceder a la base de datos. Verifica que el archivo no esté en uso e inténtalo de nuevo.";

    public static bool IsExpected(Exception exception) =>
        exception is DuplicateUserException
            or UserNotFoundException
            or DuplicateMangaException
            or MangaNotFoundException
            or DuplicateFavoriteException
            or UnauthorizedAccessException
            or KeyNotFoundException
            or ArgumentException;

    public static string Translate(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            // MangaEntry.UpdateProgress usa ThrowIfGreaterThan (mensaje técnico en inglés).
            ArgumentOutOfRangeException { ParamName: "newChapter" or "currentChapter" } => ErrorMessages.ChapterExceedsTotal,
            ArgumentException argument => StripParameterName(argument),
            DuplicateUserException or UserNotFoundException or DuplicateMangaException
                or MangaNotFoundException or DuplicateFavoriteException
                or UnauthorizedAccessException or KeyNotFoundException => exception.Message,
            SqliteException => DataAccessError,
            _ => UnexpectedError
        };
    }

    private static string StripParameterName(ArgumentException exception)
    {
        string message = exception.Message;
        if (!string.IsNullOrEmpty(exception.ParamName))
            message = message.Replace($" (Parameter '{exception.ParamName}')", "", StringComparison.Ordinal);

        int actualValueIndex = message.IndexOf(Environment.NewLine + "Actual value was", StringComparison.Ordinal);
        if (actualValueIndex >= 0)
            message = message[..actualValueIndex];

        return message.Trim();
    }
}
