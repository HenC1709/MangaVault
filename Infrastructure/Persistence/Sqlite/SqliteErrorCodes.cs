using Microsoft.Data.Sqlite;

namespace MangaVault.Infrastructure.Persistence.Sqlite;

internal static class SqliteErrorCodes
{
    public const int Constraint = 19;
    public const int ForeignKeyViolation = 787;
    public const int PrimaryKeyViolation = 1555;
    public const int UniqueViolation = 2067;

    public static bool IsPrimaryKeyViolation(SqliteException ex) =>
        ex.SqliteExtendedErrorCode == PrimaryKeyViolation
        || (ex.SqliteErrorCode == Constraint
            && ex.Message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase));

    public static bool IsUniqueViolation(SqliteException ex) =>
        ex.SqliteExtendedErrorCode == UniqueViolation
        || (ex.SqliteErrorCode == Constraint
            && ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase));

    public static bool IsForeignKeyViolation(SqliteException ex) =>
        ex.SqliteExtendedErrorCode == ForeignKeyViolation
        || (ex.SqliteErrorCode == Constraint
            && ex.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase));
}
