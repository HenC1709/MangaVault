using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Infrastructure.Persistence.Json;

namespace MangaVault.Infrastructure.Persistence.Sqlite;

public sealed record JsonImportResult(
    int UsersImported,
    int UsersSkipped,
    int WorksImported,
    int WorksSkipped,
    int EntriesImported,
    int EntriesSkipped)
{
    public bool HasImportedAny => UsersImported + WorksImported + EntriesImported > 0;
    public int TotalProcessed =>
        UsersImported + UsersSkipped + WorksImported + WorksSkipped + EntriesImported + EntriesSkipped;
}

public sealed class JsonToSqliteImporter(SqliteDatabase database)
{
    private readonly SqliteDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    public static bool HasJsonDataFiles(string sourceDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return false;

        return File.Exists(Path.Combine(sourceDirectory, "users.json"))
            || File.Exists(Path.Combine(sourceDirectory, "mangas.json"))
            || File.Exists(Path.Combine(sourceDirectory, "favorites.json"));
    }

    public JsonImportResult Import(string sourceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);

        var jsonUsers = new JsonUserRepository(sourceDirectory);
        var jsonMangas = new JsonMangaRepository(sourceDirectory);
        var jsonFavorites = new JsonFavoriteRepository(sourceDirectory);

        var sqliteUsers = new SqliteUserRepository(_database);
        var sqliteMangas = new SqliteMangaRepository(_database);
        var sqliteFavorites = new SqliteFavoriteRepository(_database);

        var userIdMap = new Dictionary<int, int>();
        var workIdMap = new Dictionary<int, int>();

        int usersImported = 0;
        int usersSkipped = 0;
        foreach (User jsonUser in jsonUsers.GetAll())
        {
            User? existing = sqliteUsers.GetByName(jsonUser.Name);
            if (existing is not null)
            {
                usersSkipped++;
                if (jsonUser.UserId > 0)
                {
                    userIdMap[jsonUser.UserId] = existing.UserId;
                }

                continue;
            }

            bool preserveId = jsonUser.UserId > 0 && sqliteUsers.GetById(jsonUser.UserId) is null;
            var toInsert = new User(
                preserveId ? jsonUser.UserId : 0,
                jsonUser.Name,
                jsonUser.PasswordHash,
                jsonUser.Role);
            sqliteUsers.Save(toInsert);
            usersImported++;

            if (jsonUser.UserId > 0)
            {
                userIdMap[jsonUser.UserId] = toInsert.UserId;
            }
        }

        List<Manga> existingWorks = sqliteMangas.GetAll();
        var worksByKey = new Dictionary<(string Title, MediaType Type), Manga>(WorkKeyComparer.Instance);
        var usedWorkIds = new HashSet<int>();
        foreach (Manga work in existingWorks)
        {
            worksByKey[(work.Title, work.Type)] = work;
            usedWorkIds.Add(work.Id);
        }

        int worksImported = 0;
        int worksSkipped = 0;
        foreach (Manga jsonWork in jsonMangas.GetAll())
        {
            if (worksByKey.TryGetValue((jsonWork.Title, jsonWork.Type), out Manga? existingWork))
            {
                worksSkipped++;
                if (jsonWork.Id > 0)
                {
                    workIdMap[jsonWork.Id] = existingWork.Id;
                }

                continue;
            }

            bool preserveId = jsonWork.Id > 0 && !usedWorkIds.Contains(jsonWork.Id);
            var toInsert = new Manga(
                id: preserveId ? jsonWork.Id : 0,
                title: jsonWork.Title,
                mangaka: jsonWork.Mangaka,
                volumeCount: jsonWork.VolumeCount,
                finalChapters: jsonWork.FinalChapters,
                status: jsonWork.Status,
                type: jsonWork.Type,
                illustrator: jsonWork.Illustrator,
                coverPath: jsonWork.CoverPath,
                synopsis: jsonWork.Synopsis);

            foreach (Genre genre in jsonWork.Genres)
            {
                toInsert.AddGenre(new Genre(genre.Name, genre.Description));
            }

            sqliteMangas.Save(toInsert);
            worksImported++;
            worksByKey[(toInsert.Title, toInsert.Type)] = toInsert;
            usedWorkIds.Add(toInsert.Id);

            if (jsonWork.Id > 0)
            {
                workIdMap[jsonWork.Id] = toInsert.Id;
            }
        }

        int entriesImported = 0;
        int entriesSkipped = 0;
        foreach (MangaEntry jsonEntry in jsonFavorites.GetAll())
        {
            int targetUserId;
            if (userIdMap.TryGetValue(jsonEntry.UserId, out int mappedUserId))
            {
                targetUserId = mappedUserId;
            }
            else if (sqliteUsers.GetById(jsonEntry.UserId) is not null)
            {
                targetUserId = jsonEntry.UserId;
            }
            else
            {
                entriesSkipped++;
                continue;
            }

            int targetWorkId;
            if (workIdMap.TryGetValue(jsonEntry.MangaId, out int mappedWorkId))
            {
                targetWorkId = mappedWorkId;
            }
            else if (usedWorkIds.Contains(jsonEntry.MangaId))
            {
                targetWorkId = jsonEntry.MangaId;
            }
            else
            {
                entriesSkipped++;
                continue;
            }

            if (sqliteFavorites.Get(targetUserId, targetWorkId) is not null)
            {
                entriesSkipped++;
                continue;
            }

            DateTime? dateAdded = jsonEntry.DateAdded == default ? null : jsonEntry.DateAdded;
            var toInsert = new MangaEntry(
                userId: targetUserId,
                mangaId: targetWorkId,
                currentChapter: jsonEntry.CurrentChapter,
                status: jsonEntry.ReadingStatus,
                ownedVolumes: jsonEntry.OwnedVolumes,
                rating: jsonEntry.Rating,
                isFavorite: jsonEntry.IsFavorite,
                dateAdded: dateAdded);

            sqliteFavorites.Save(toInsert);
            entriesImported++;
        }

        return new JsonImportResult(
            UsersImported: usersImported,
            UsersSkipped: usersSkipped,
            WorksImported: worksImported,
            WorksSkipped: worksSkipped,
            EntriesImported: entriesImported,
            EntriesSkipped: entriesSkipped);
    }

    private sealed class WorkKeyComparer : IEqualityComparer<(string Title, MediaType Type)>
    {
        public static readonly WorkKeyComparer Instance = new();

        public bool Equals((string Title, MediaType Type) x, (string Title, MediaType Type) y) =>
            x.Type == y.Type && string.Equals(x.Title, y.Title, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Title, MediaType Type) obj) =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Title), obj.Type);
    }
}
