namespace MangaVault.Core.Constants;

public static class ErrorMessages
{
    public const string UsernameRequired = "El nombre de usuario es obligatorio.";
    public static readonly string UsernameLengthInvalid =
        $"El nombre de usuario debe tener entre {ValidationRules.MinUsernameLength} y {ValidationRules.MaxUsernameLength} caracteres.";

    public const string PasswordRequired = "La contraseña es obligatoria.";
    public static readonly string PasswordLengthInvalid =
        $"La contraseña debe tener entre {ValidationRules.MinPasswordLength} y {ValidationRules.MaxPasswordLength} caracteres.";

    public const string InvalidRole = "El rol de usuario no es válido.";
    public const string InvalidCredentials = "Usuario o contraseña incorrectos.";
    public const string InvalidCurrentPassword = "La contraseña actual es incorrecta.";

    public const string TitleRequired = "El título de la obra es obligatorio.";
    public static readonly string TitleTooLong =
        $"El título no puede superar los {ValidationRules.MaxTitleLength} caracteres.";
    public static readonly string AuthorTooLong =
        $"El nombre del autor no puede superar los {ValidationRules.MaxAuthorLength} caracteres.";

    public const string NegativeVolumes = "La cantidad de tomos no puede ser negativa.";
    public const string NegativeChapters = "La cantidad de capítulos no puede ser negativa.";
    public const string InvalidMangaStatus = "El estado de publicación no es válido.";
    public const string InvalidMediaType = "El tipo de obra no es válido.";

    public const string EntryNotFound = "No se encontró el registro en la biblioteca del usuario.";
    public const string ChapterExceedsTotal = "El capítulo actual no puede superar el total de capítulos de la obra.";
}
