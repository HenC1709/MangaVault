using System.Globalization;
using MangaVault.Core.Constants;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace MangaVault.Infrastructure.Persistence.Sqlite;

public sealed class SqliteFavoriteRepository(SqliteDatabase database) : IFavoriteRepository
{
    private readonly SqliteDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    public List<MangaEntry> GetAll()
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT UserId, WorkId, ReadingStatus, CurrentChapter, OwnedVolumes, Rating, IsFavorite, DateAdded
            FROM UserWorks
            ORDER BY UserId, WorkId;
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        var entries = new List<MangaEntry>();
        while (reader.Read())
        {
            entries.Add(MapEntry(reader));
        }

        return entries;
    }

    public List<MangaEntry> GetByUser(int userId)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT UserId, WorkId, ReadingStatus, CurrentChapter, OwnedVolumes, Rating, IsFavorite, DateAdded
            FROM UserWorks
            WHERE UserId = @userId
            ORDER BY WorkId;
            """;
        command.Parameters.AddWithValue("@userId", userId);

        using SqliteDataReader reader = command.ExecuteReader();
        var entries = new List<MangaEntry>();
        while (reader.Read())
        {
            entries.Add(MapEntry(reader));
        }

        return entries;
    }

    public MangaEntry? Get(int userId, int mangaId)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT UserId, WorkId, ReadingStatus, CurrentChapter, OwnedVolumes, Rating, IsFavorite, DateAdded
            FROM UserWorks
            WHERE UserId = @userId AND WorkId = @workId;
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@workId", mangaId);

        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? MapEntry(reader) : null;
    }

    public void Save(MangaEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO UserWorks (UserId, WorkId, ReadingStatus, CurrentChapter, OwnedVolumes, Rating, IsFavorite, DateAdded)
            VALUES (@userId, @workId, @readingStatus, @currentChapter, @ownedVolumes, @rating, @isFavorite, @dateAdded);
            """;
        command.Parameters.AddWithValue("@userId", entry.UserId);
        command.Parameters.AddWithValue("@workId", entry.MangaId);
        command.Parameters.AddWithValue("@readingStatus", (int)entry.ReadingStatus);
        command.Parameters.AddWithValue("@currentChapter", entry.CurrentChapter);
        command.Parameters.AddWithValue("@ownedVolumes", entry.OwnedVolumes);
        command.Parameters.AddWithValue("@rating", (object?)entry.Rating ?? DBNull.Value);
        command.Parameters.AddWithValue("@isFavorite", entry.IsFavorite ? 1 : 0);
        command.Parameters.AddWithValue("@dateAdded", entry.DateAdded.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        try
        {
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (SqliteErrorCodes.IsPrimaryKeyViolation(ex) || SqliteErrorCodes.IsUniqueViolation(ex))
        {
            throw new DuplicateFavoriteException(entry.UserId, entry.MangaId);
        }
        catch (SqliteException ex) when (SqliteErrorCodes.IsForeignKeyViolation(ex))
        {
            ThrowMissingForeignKeyEntity(connection, entry.UserId, entry.MangaId);
        }
    }

    public void Update(MangaEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE UserWorks
            SET ReadingStatus = @readingStatus,
                CurrentChapter = @currentChapter,
                OwnedVolumes = @ownedVolumes,
                Rating = @rating,
                IsFavorite = @isFavorite
            WHERE UserId = @userId AND WorkId = @workId;
            """;
        command.Parameters.AddWithValue("@userId", entry.UserId);
        command.Parameters.AddWithValue("@workId", entry.MangaId);
        command.Parameters.AddWithValue("@readingStatus", (int)entry.ReadingStatus);
        command.Parameters.AddWithValue("@currentChapter", entry.CurrentChapter);
        command.Parameters.AddWithValue("@ownedVolumes", entry.OwnedVolumes);
        command.Parameters.AddWithValue("@rating", (object?)entry.Rating ?? DBNull.Value);
        command.Parameters.AddWithValue("@isFavorite", entry.IsFavorite ? 1 : 0);

        int affected = command.ExecuteNonQuery();
        if (affected == 0)
            throw new KeyNotFoundException(ErrorMessages.EntryNotFound);
    }

    public void Delete(int userId, int mangaId)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM UserWorks
            WHERE UserId = @userId AND WorkId = @workId;
            """;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@workId", mangaId);

        int affected = command.ExecuteNonQuery();
        if (affected == 0)
            throw new KeyNotFoundException(ErrorMessages.EntryNotFound);
    }

    private static void ThrowMissingForeignKeyEntity(SqliteConnection connection, int userId, int mangaId)
    {
        using SqliteCommand checkUser = connection.CreateCommand();
        checkUser.CommandText = "SELECT EXISTS(SELECT 1 FROM Users WHERE UserId = @userId);";
        checkUser.Parameters.AddWithValue("@userId", userId);

        long exists = Convert.ToInt64(checkUser.ExecuteScalar());
        if (exists == 0)
            throw new UserNotFoundException(userId);

        throw new MangaNotFoundException(mangaId);
    }

    private static MangaEntry MapEntry(SqliteDataReader reader)
    {
        string rawDate = reader.GetString(7);
        DateTime parsedDate = DateTime.Parse(
            rawDate,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

        return new MangaEntry(
            userId: reader.GetInt32(0),
            mangaId: reader.GetInt32(1),
            currentChapter: reader.GetInt32(3),
            status: (ReadingStatus)reader.GetInt32(2),
            ownedVolumes: reader.GetInt32(4),
            rating: reader.IsDBNull(5) ? null : reader.GetInt32(5),
            isFavorite: reader.GetInt32(6) != 0,
            dateAdded: parsedDate);
    }
}
