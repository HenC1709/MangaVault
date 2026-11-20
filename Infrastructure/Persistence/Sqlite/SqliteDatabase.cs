using Microsoft.Data.Sqlite;

namespace MangaVault.Infrastructure.Persistence.Sqlite;

public sealed class SqliteDatabase : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection? _keepAliveConnection;
    private bool _disposed;

    public string DatabasePath { get; }
    public bool IsInMemory { get; }

    public SqliteDatabase(string? databasePath = null)
    {
        DatabasePath = string.IsNullOrWhiteSpace(databasePath)
            ? GetDefaultDatabasePath()
            : databasePath;
        IsInMemory = false;

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            DefaultTimeout = 5
        }.ToString();
    }

    private SqliteDatabase(string databasePath, string connectionString, SqliteConnection keepAliveConnection)
    {
        DatabasePath = databasePath;
        IsInMemory = true;
        _connectionString = connectionString;
        _keepAliveConnection = keepAliveConnection;
    }

    public static string GetDefaultDatabasePath(string? dataDirectory = null) =>
        Path.Combine(
            dataDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MangaVault"),
            "mangavault.db");

    public static SqliteDatabase CreateInMemory()
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"mangavault-mem-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true
        }.ToString();

        var keepAliveConnection = new SqliteConnection(connectionString);
        try
        {
            keepAliveConnection.Open();
            EnableForeignKeys(keepAliveConnection);
            return new SqliteDatabase(":memory:", connectionString, keepAliveConnection);
        }
        catch
        {
            keepAliveConnection.Dispose();
            throw;
        }
    }

    public SqliteConnection OpenConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsInMemory)
        {
            string? directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        var connection = new SqliteConnection(_connectionString);
        try
        {
            connection.Open();
            EnableForeignKeys(connection);
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public void Initialize(bool seedGenres = true) =>
        SqliteSchemaInitializer.Initialize(this, seedGenres);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _keepAliveConnection?.Dispose();

        if (!IsInMemory)
        {
            using var conn = new SqliteConnection(_connectionString);
            SqliteConnection.ClearPool(conn);
        }
    }

    private static void EnableForeignKeys(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
    }
}
