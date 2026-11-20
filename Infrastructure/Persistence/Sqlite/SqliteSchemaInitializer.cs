using MangaVault.Core.Entities;
using Microsoft.Data.Sqlite;

namespace MangaVault.Infrastructure.Persistence.Sqlite;

public static class SqliteSchemaInitializer
{
    public static readonly IReadOnlyList<Genre> DefaultGenres =
    [
        new("Acción", "Combates, ritmo intenso y enfrentamientos constantes."),
        new("Aventura", "Viajes, exploración y descubrimiento de nuevos mundos."),
        new("Ciencia Ficción", "Tecnología avanzada, futuro, espacio o distopías."),
        new("Comedia", "Situaciones humorísticas y tono ligero."),
        new("Deportes", "Competencia atlética, superación y trabajo en equipo."),
        new("Drama", "Conflictos emocionales y desarrollo profundo de personajes."),
        new("Fantasía", "Magia, criaturas míticas y mundos imaginarios."),
        new("Histórico", "Ambientación en épocas o sucesos del pasado."),
        new("Isekai", "Protagonista transportado o reencarnado en otro mundo."),
        new("Josei", "Historias dirigidas principalmente a público femenino adulto."),
        new("Mecha", "Robots gigantes y vehículos tripulados de combate."),
        new("Misterio", "Enigmas, investigaciones y secretos por resolver."),
        new("Psicológico", "Tensión mental, dilemas morales y mente humana."),
        new("Romance", "Relaciones afectivas y vínculos sentimentales."),
        new("Seinen", "Obras con temática madura para público adulto joven."),
        new("Shojo", "Historias centradas en emociones y público juvenil femenino."),
        new("Shonen", "Aventura, amistad y superación para público juvenil."),
        new("Slice of Life", "Vida cotidiana, calma y relaciones diarias."),
        new("Sobrenatural", "Fenómenos paranormales, espíritus y poderes ocultos."),
        new("Terror", "Suspenso, horror y atmósfera inquietante.")
    ];

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS Users (
            UserId       INTEGER PRIMARY KEY AUTOINCREMENT,
            Name         TEXT NOT NULL UNIQUE COLLATE NOCASE,
            PasswordHash TEXT NOT NULL,
            Role         INTEGER NOT NULL DEFAULT 0,
            CreatedAt    TEXT NOT NULL DEFAULT (datetime('now'))
        );

        CREATE TABLE IF NOT EXISTS Works (
            WorkId        INTEGER PRIMARY KEY AUTOINCREMENT,
            Type          INTEGER NOT NULL DEFAULT 0,
            Title         TEXT NOT NULL COLLATE NOCASE,
            Author        TEXT NOT NULL DEFAULT '',
            Illustrator   TEXT,
            VolumeCount   INTEGER NOT NULL DEFAULT 0 CHECK (VolumeCount >= 0),
            FinalChapters INTEGER NOT NULL DEFAULT 0 CHECK (FinalChapters >= 0),
            Status        INTEGER NOT NULL DEFAULT 0,
            CoverPath     TEXT,
            Synopsis      TEXT,
            UNIQUE (Title, Type)
        );

        CREATE TABLE IF NOT EXISTS Genres (
            GenreId     INTEGER PRIMARY KEY AUTOINCREMENT,
            Name        TEXT NOT NULL UNIQUE COLLATE NOCASE,
            Description TEXT NOT NULL DEFAULT ''
        );

        CREATE TABLE IF NOT EXISTS WorkGenres (
            WorkId  INTEGER NOT NULL REFERENCES Works(WorkId) ON DELETE CASCADE,
            GenreId INTEGER NOT NULL REFERENCES Genres(GenreId) ON DELETE CASCADE,
            PRIMARY KEY (WorkId, GenreId)
        );

        CREATE TABLE IF NOT EXISTS UserWorks (
            UserId         INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
            WorkId         INTEGER NOT NULL REFERENCES Works(WorkId) ON DELETE CASCADE,
            ReadingStatus  INTEGER NOT NULL DEFAULT 2,
            CurrentChapter INTEGER NOT NULL DEFAULT 0 CHECK (CurrentChapter >= 0),
            OwnedVolumes   INTEGER NOT NULL DEFAULT 0 CHECK (OwnedVolumes >= 0),
            Rating         INTEGER CHECK (Rating BETWEEN 1 AND 10),
            IsFavorite     INTEGER NOT NULL DEFAULT 0 CHECK (IsFavorite IN (0, 1)),
            DateAdded      TEXT NOT NULL DEFAULT (datetime('now')),
            PRIMARY KEY (UserId, WorkId)
        );

        CREATE TABLE IF NOT EXISTS Collections (
            CollectionId INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId       INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
            Name         TEXT NOT NULL COLLATE NOCASE,
            UNIQUE (UserId, Name)
        );

        CREATE TABLE IF NOT EXISTS CollectionWorks (
            CollectionId INTEGER NOT NULL REFERENCES Collections(CollectionId) ON DELETE CASCADE,
            WorkId       INTEGER NOT NULL REFERENCES Works(WorkId) ON DELETE CASCADE,
            PRIMARY KEY (CollectionId, WorkId)
        );

        CREATE INDEX IF NOT EXISTS IX_Works_Title ON Works(Title);
        CREATE INDEX IF NOT EXISTS IX_UserWorks_Status ON UserWorks(UserId, ReadingStatus);
        """;

    public static void Initialize(SqliteDatabase database, bool seedGenres = true)
    {
        ArgumentNullException.ThrowIfNull(database);

        using SqliteConnection connection = database.OpenConnection();

        if (!database.IsInMemory)
        {
            using SqliteCommand walCommand = connection.CreateCommand();
            walCommand.CommandText = "PRAGMA journal_mode = WAL;";
            walCommand.ExecuteNonQuery();
        }

        using SqliteTransaction transaction = connection.BeginTransaction();

        using (SqliteCommand schemaCommand = connection.CreateCommand())
        {
            schemaCommand.Transaction = transaction;
            schemaCommand.CommandText = SchemaSql;
            schemaCommand.ExecuteNonQuery();
        }

        if (seedGenres)
        {
            using SqliteCommand seedCommand = connection.CreateCommand();
            seedCommand.Transaction = transaction;
            seedCommand.CommandText = "INSERT OR IGNORE INTO Genres (Name, Description) VALUES (@name, @description);";

            SqliteParameter nameParam = seedCommand.Parameters.Add("@name", SqliteType.Text);
            SqliteParameter descParam = seedCommand.Parameters.Add("@description", SqliteType.Text);

            foreach (Genre genre in DefaultGenres)
            {
                nameParam.Value = genre.Name;
                descParam.Value = genre.Description;
                seedCommand.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }
}
