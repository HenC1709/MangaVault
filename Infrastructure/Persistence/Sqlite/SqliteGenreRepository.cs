using MangaVault.Core.Entities;
using MangaVault.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace MangaVault.Infrastructure.Persistence.Sqlite;

public sealed class SqliteGenreRepository(SqliteDatabase database) : IGenreRepository
{
    private readonly SqliteDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    public List<Genre> GetAll()
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Name, Description
            FROM Genres
            ORDER BY Name COLLATE NOCASE;
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        var genres = new List<Genre>();
        while (reader.Read())
        {
            genres.Add(new Genre(reader.GetString(0), reader.GetString(1)));
        }

        return genres;
    }

    public Genre? GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT Name, Description
            FROM Genres
            WHERE Name = @name COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("@name", name.Trim());

        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? new Genre(reader.GetString(0), reader.GetString(1)) : null;
    }

    public void Save(Genre genre)
    {
        ArgumentNullException.ThrowIfNull(genre);

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Genres (Name, Description)
            VALUES (@name, @description)
            ON CONFLICT(Name) DO UPDATE SET
                Description = CASE
                    WHEN excluded.Description <> '' THEN excluded.Description
                    ELSE Genres.Description
                END;
            """;
        command.Parameters.AddWithValue("@name", genre.Name);
        command.Parameters.AddWithValue("@description", genre.Description);
        command.ExecuteNonQuery();
    }
}
