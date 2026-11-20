using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace MangaVault.Infrastructure.Persistence.Sqlite;

public sealed class SqliteMangaRepository(SqliteDatabase database) : IMangaRepository
{
    private readonly SqliteDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    public List<Manga> GetAll()
    {
        using SqliteConnection connection = _database.OpenConnection();

        var works = new List<Manga>();
        var worksById = new Dictionary<int, Manga>();

        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT WorkId, Type, Title, Author, Illustrator, VolumeCount, FinalChapters, Status, CoverPath, Synopsis
                FROM Works
                ORDER BY WorkId;
                """;

            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Manga work = MapWork(reader);
                works.Add(work);
                worksById[work.Id] = work;
            }
        }

        if (works.Count == 0)
            return works;

        using (SqliteCommand genreCommand = connection.CreateCommand())
        {
            genreCommand.CommandText = """
                SELECT wg.WorkId, g.Name, g.Description
                FROM WorkGenres wg
                INNER JOIN Genres g ON g.GenreId = wg.GenreId
                ORDER BY g.Name COLLATE NOCASE;
                """;

            using SqliteDataReader reader = genreCommand.ExecuteReader();
            while (reader.Read())
            {
                int workId = reader.GetInt32(0);
                if (worksById.TryGetValue(workId, out Manga? work))
                {
                    work.AddGenre(new Genre(reader.GetString(1), reader.GetString(2)));
                }
            }
        }

        return works;
    }

    public Manga? GetById(int id)
    {
        using SqliteConnection connection = _database.OpenConnection();

        Manga? work = null;
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT WorkId, Type, Title, Author, Illustrator, VolumeCount, FinalChapters, Status, CoverPath, Synopsis
                FROM Works
                WHERE WorkId = @workId;
                """;
            command.Parameters.AddWithValue("@workId", id);

            using SqliteDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                work = MapWork(reader);
            }
        }

        if (work is null)
            return null;

        using (SqliteCommand genreCommand = connection.CreateCommand())
        {
            genreCommand.CommandText = """
                SELECT g.Name, g.Description
                FROM WorkGenres wg
                INNER JOIN Genres g ON g.GenreId = wg.GenreId
                WHERE wg.WorkId = @workId
                ORDER BY g.Name COLLATE NOCASE;
                """;
            genreCommand.Parameters.AddWithValue("@workId", id);

            using SqliteDataReader reader = genreCommand.ExecuteReader();
            while (reader.Read())
            {
                work.AddGenre(new Genre(reader.GetString(0), reader.GetString(1)));
            }
        }

        return work;
    }

    public void Save(Manga manga)
    {
        ArgumentNullException.ThrowIfNull(manga);

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteTransaction transaction = connection.BeginTransaction();

        bool assignGeneratedId = manga.Id <= 0;
        int workId = manga.Id;

        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = assignGeneratedId
                ? """
                  INSERT INTO Works (Type, Title, Author, Illustrator, VolumeCount, FinalChapters, Status, CoverPath, Synopsis)
                  VALUES (@type, @title, @author, @illustrator, @volumeCount, @finalChapters, @status, @coverPath, @synopsis);
                  SELECT last_insert_rowid();
                  """
                : """
                  INSERT INTO Works (WorkId, Type, Title, Author, Illustrator, VolumeCount, FinalChapters, Status, CoverPath, Synopsis)
                  VALUES (@workId, @type, @title, @author, @illustrator, @volumeCount, @finalChapters, @status, @coverPath, @synopsis);
                  """;

            if (!assignGeneratedId)
            {
                command.Parameters.AddWithValue("@workId", manga.Id);
            }

            BindWorkParameters(command, manga);

            try
            {
                if (assignGeneratedId)
                {
                    object? scalar = command.ExecuteScalar();
                    workId = Convert.ToInt32(scalar);
                }
                else
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (SqliteException ex) when (SqliteErrorCodes.IsPrimaryKeyViolation(ex))
            {
                throw new DuplicateMangaException(manga.Id);
            }
            catch (SqliteException ex) when (SqliteErrorCodes.IsUniqueViolation(ex))
            {
                throw new DuplicateMangaException(manga.Title, manga.Type);
            }
        }

        SaveWorkGenres(connection, transaction, workId, manga.Genres);
        transaction.Commit();

        if (assignGeneratedId)
        {
            manga.Id = workId;
        }
    }

    public void Update(Manga manga)
    {
        ArgumentNullException.ThrowIfNull(manga);

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteTransaction transaction = connection.BeginTransaction();

        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE Works
                SET Type = @type,
                    Title = @title,
                    Author = @author,
                    Illustrator = @illustrator,
                    VolumeCount = @volumeCount,
                    FinalChapters = @finalChapters,
                    Status = @status,
                    CoverPath = @coverPath,
                    Synopsis = @synopsis
                WHERE WorkId = @workId;
                """;
            command.Parameters.AddWithValue("@workId", manga.Id);
            BindWorkParameters(command, manga);

            int affected;
            try
            {
                affected = command.ExecuteNonQuery();
            }
            catch (SqliteException ex) when (SqliteErrorCodes.IsUniqueViolation(ex))
            {
                throw new DuplicateMangaException(manga.Title, manga.Type);
            }

            if (affected == 0)
                throw new MangaNotFoundException(manga.Id);
        }

        using (SqliteCommand deleteGenres = connection.CreateCommand())
        {
            deleteGenres.Transaction = transaction;
            deleteGenres.CommandText = "DELETE FROM WorkGenres WHERE WorkId = @workId;";
            deleteGenres.Parameters.AddWithValue("@workId", manga.Id);
            deleteGenres.ExecuteNonQuery();
        }

        SaveWorkGenres(connection, transaction, manga.Id, manga.Genres);
        transaction.Commit();
    }

    public void Delete(int id)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Works WHERE WorkId = @workId;";
        command.Parameters.AddWithValue("@workId", id);

        int affected = command.ExecuteNonQuery();
        if (affected == 0)
            throw new MangaNotFoundException(id);
    }

    private static void BindWorkParameters(SqliteCommand command, Manga manga)
    {
        command.Parameters.AddWithValue("@type", (int)manga.Type);
        command.Parameters.AddWithValue("@title", manga.Title);
        command.Parameters.AddWithValue("@author", manga.Mangaka);
        command.Parameters.AddWithValue("@illustrator", (object?)manga.Illustrator ?? DBNull.Value);
        command.Parameters.AddWithValue("@volumeCount", manga.VolumeCount);
        command.Parameters.AddWithValue("@finalChapters", manga.FinalChapters);
        command.Parameters.AddWithValue("@status", (int)manga.Status);
        command.Parameters.AddWithValue("@coverPath", (object?)manga.CoverPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@synopsis", (object?)manga.Synopsis ?? DBNull.Value);
    }

    private static void SaveWorkGenres(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int workId,
        IReadOnlyList<Genre> genres)
    {
        if (genres.Count == 0)
            return;

        using SqliteCommand insertGenre = connection.CreateCommand();
        insertGenre.Transaction = transaction;
        insertGenre.CommandText = "INSERT OR IGNORE INTO Genres (Name, Description) VALUES (@name, @description);";
        SqliteParameter insertNameParam = insertGenre.Parameters.Add("@name", SqliteType.Text);
        SqliteParameter insertDescParam = insertGenre.Parameters.Add("@description", SqliteType.Text);

        using SqliteCommand selectGenreId = connection.CreateCommand();
        selectGenreId.Transaction = transaction;
        selectGenreId.CommandText = "SELECT GenreId FROM Genres WHERE Name = @name COLLATE NOCASE;";
        SqliteParameter selectNameParam = selectGenreId.Parameters.Add("@name", SqliteType.Text);

        using SqliteCommand linkWorkGenre = connection.CreateCommand();
        linkWorkGenre.Transaction = transaction;
        linkWorkGenre.CommandText = "INSERT OR IGNORE INTO WorkGenres (WorkId, GenreId) VALUES (@workId, @genreId);";
        linkWorkGenre.Parameters.AddWithValue("@workId", workId);
        SqliteParameter genreIdParam = linkWorkGenre.Parameters.Add("@genreId", SqliteType.Integer);

        foreach (Genre genre in genres)
        {
            insertNameParam.Value = genre.Name;
            insertDescParam.Value = genre.Description;
            insertGenre.ExecuteNonQuery();

            selectNameParam.Value = genre.Name;
            long genreId = Convert.ToInt64(selectGenreId.ExecuteScalar());

            genreIdParam.Value = genreId;
            linkWorkGenre.ExecuteNonQuery();
        }
    }

    private static Manga MapWork(SqliteDataReader reader) =>
        new(
            id: reader.GetInt32(0),
            type: (MediaType)reader.GetInt32(1),
            title: reader.GetString(2),
            mangaka: reader.GetString(3),
            illustrator: reader.IsDBNull(4) ? null : reader.GetString(4),
            volumeCount: reader.GetInt32(5),
            finalChapters: reader.GetInt32(6),
            status: (MangaStatus)reader.GetInt32(7),
            coverPath: reader.IsDBNull(8) ? null : reader.GetString(8),
            synopsis: reader.IsDBNull(9) ? null : reader.GetString(9));
}
