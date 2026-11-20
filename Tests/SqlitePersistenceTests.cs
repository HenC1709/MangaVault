using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Infrastructure.Persistence.Json;
using MangaVault.Infrastructure.Persistence.Sqlite;
using MangaVault.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MangaVault.Tests;

public sealed class SqlitePersistenceTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (string dir in _tempDirs)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    private string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"mangavault-sqlite-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        _tempDirs.Add(path);
        return path;
    }

    [Fact]
    public void Initialize_CreatesSchemaIndexesForeignKeysAndSeedsGenresIdempotently()
    {
        using SqliteDatabase database = SqliteDatabase.CreateInMemory();
        database.Initialize();
        database.Initialize();

        using SqliteConnection connection = database.OpenConnection();

        using (SqliteCommand fkCommand = connection.CreateCommand())
        {
            fkCommand.CommandText = "PRAGMA foreign_keys;";
            long foreignKeysEnabled = Convert.ToInt64(fkCommand.ExecuteScalar());
            Assert.Equal(1L, foreignKeysEnabled);
        }

        var objects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (SqliteCommand schemaCommand = connection.CreateCommand())
        {
            schemaCommand.CommandText = """
                SELECT name
                FROM sqlite_master
                WHERE type IN ('table', 'index');
                """;
            using SqliteDataReader reader = schemaCommand.ExecuteReader();
            while (reader.Read())
            {
                objects.Add(reader.GetString(0));
            }
        }

        string[] expectedObjects =
        [
            "Users",
            "Works",
            "Genres",
            "WorkGenres",
            "UserWorks",
            "Collections",
            "CollectionWorks",
            "IX_Works_Title",
            "IX_UserWorks_Status"
        ];

        foreach (string expected in expectedObjects)
        {
            Assert.Contains(expected, objects);
        }

        var genreRepo = new SqliteGenreRepository(database);
        List<Genre> genres = genreRepo.GetAll();
        Assert.Equal(SqliteSchemaInitializer.DefaultGenres.Count, genres.Count);
        Assert.NotNull(genreRepo.GetByName("fantasía"));
        Assert.NotNull(genreRepo.GetByName("ISEKAI"));
    }

    [Fact]
    public void UserService_WithSqlite_FullLifecycleAndCaseInsensitiveUniqueness()
    {
        using SqliteDatabase database = SqliteDatabase.CreateInMemory();
        database.Initialize(seedGenres: false);

        var userRepo = new SqliteUserRepository(database);
        var userService = new UserService(userRepo, new Pbkdf2PasswordHasher());

        User created = userService.Register(new CreateUserDto("  Kirito  ", "secret123", Role.Admin));
        Assert.Equal(1, created.UserId);
        Assert.Equal("Kirito", created.Name);
        Assert.Equal(Role.Admin, created.Role);

        Session session = userService.Authenticate("kirito", "secret123");
        Assert.Equal(created.UserId, session.UserId);
        Assert.Equal("Kirito", session.UserName);
        Assert.Equal(Role.Admin, session.Role);

        userService.ChangePassword(created.UserId, "secret123", "newpass456");
        Assert.Throws<UnauthorizedAccessException>(() => userService.Authenticate("Kirito", "secret123"));
        Assert.Equal(created.UserId, userService.Authenticate("KIRITO", "newpass456").UserId);

        Assert.Throws<DuplicateUserException>(() =>
            userService.Register(new CreateUserDto("KIRITO", "another123")));

        var missing = new User(999, "Ghost", "hash-value");
        Assert.Throws<UserNotFoundException>(() => userRepo.Update(missing));
    }

    [Fact]
    public void MangaService_WithSqlite_CrudGenresAndMediaTypeUniqueness()
    {
        using SqliteDatabase database = SqliteDatabase.CreateInMemory();
        database.Initialize();

        var mangaRepo = new SqliteMangaRepository(database);
        var genreRepo = new SqliteGenreRepository(database);
        var mangaService = new MangaService(mangaRepo);

        Manga manga = mangaService.Create(new CreateMangaDto(
            Title: "Overlord",
            Mangaka: "Kugane Maruyama",
            VolumeCount: 19,
            FinalChapters: 78,
            Status: MangaStatus.Ongoing,
            Type: MediaType.Manga,
            Genres: ["Fantasía", "Dark Fantasy"]));

        Manga novel = mangaService.Create(new CreateMangaDto(
            Title: "Overlord",
            Mangaka: "Kugane Maruyama",
            VolumeCount: 16,
            FinalChapters: 0,
            Status: MangaStatus.Ongoing,
            Type: MediaType.LightNovel,
            Illustrator: "so-bin",
            CoverPath: "covers/overlord-ln.jpg",
            Synopsis: "Ainz Ooal Gown en el Nuevo Mundo.",
            Genres: ["Isekai", "Fantasía"]));

        Assert.NotEqual(manga.Id, novel.Id);
        Assert.NotNull(genreRepo.GetByName("Dark Fantasy"));

        Manga persistedNovel = mangaService.GetById(novel.Id);
        Assert.Equal(MediaType.LightNovel, persistedNovel.Type);
        Assert.Equal("so-bin", persistedNovel.Illustrator);
        Assert.Equal("covers/overlord-ln.jpg", persistedNovel.CoverPath);
        Assert.Equal("Ainz Ooal Gown en el Nuevo Mundo.", persistedNovel.Synopsis);
        Assert.Equal(2, persistedNovel.Genres.Count);

        Assert.Throws<DuplicateMangaException>(() =>
            mangaService.Create(new CreateMangaDto("overlord", "Other", Type: MediaType.Manga)));

        Manga other = mangaService.Create(new CreateMangaDto("Youjo Senki", "Carlo Zen", Type: MediaType.LightNovel));
        Assert.Throws<DuplicateMangaException>(() => mangaService.UpdateTitle(other.Id, "OVERLORD"));

        Manga updated = mangaService.Update(manga.Id, new CreateMangaDto(
            Title: "Overlord (Manga)",
            Mangaka: "Hugin Miyama",
            VolumeCount: 20,
            FinalChapters: 82,
            Status: MangaStatus.Completed,
            Type: MediaType.Manga,
            Genres: ["Isekai"]));

        Assert.Single(updated.Genres);
        Manga reloaded = mangaService.GetById(manga.Id);
        Assert.Equal("Overlord (Manga)", reloaded.Title);
        Assert.Equal(MangaStatus.Completed, reloaded.Status);
        Assert.Single(reloaded.Genres);
        Assert.Equal("Isekai", reloaded.Genres[0].Name);

        IReadOnlyList<Manga> novelMatches = mangaService.SearchByTitle("lord", MediaType.LightNovel);
        Assert.Single(novelMatches);
        Assert.Equal(novel.Id, novelMatches[0].Id);

        Assert.Throws<MangaNotFoundException>(() => mangaService.Delete(9999));
    }

    [Fact]
    public void FavoriteService_WithSqlite_LibraryProgressRatingVolumesAndCascadeDelete()
    {
        using SqliteDatabase database = SqliteDatabase.CreateInMemory();
        database.Initialize(seedGenres: false);

        var userRepo = new SqliteUserRepository(database);
        var mangaRepo = new SqliteMangaRepository(database);
        var favRepo = new SqliteFavoriteRepository(database);

        var userService = new UserService(userRepo, new Pbkdf2PasswordHasher());
        var mangaService = new MangaService(mangaRepo);
        var favoriteService = new FavoriteService(favRepo, userRepo, mangaRepo);

        User user = userService.Register(new CreateUserDto("Subaru", "returnbydeath"));
        Manga work = mangaService.Create(new CreateMangaDto(
            "Monster",
            "Naoki Urasawa",
            FinalChapters: 162,
            Genres: ["Misterio", "Psicológico"]));

        MangaEntry added = favoriteService.AddToLibrary(user.UserId, work.Id, ReadingStatus.Reading, 10, isFavorite: false);
        Assert.Equal(DateTimeKind.Utc, added.DateAdded.Kind);

        Assert.Throws<DuplicateFavoriteException>(() =>
            favoriteService.AddToLibrary(user.UserId, work.Id));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            favoriteService.UpdateProgress(user.UserId, work.Id, 163, ReadingStatus.Reading));

        MangaEntry progressed = favoriteService.UpdateProgress(user.UserId, work.Id, 162, ReadingStatus.Completed);
        Assert.Equal(162, progressed.CurrentChapter);
        Assert.Equal(ReadingStatus.Completed, progressed.ReadingStatus);

        favoriteService.SetFavorite(user.UserId, work.Id, true);
        favoriteService.SetRating(user.UserId, work.Id, 9);
        favoriteService.SetOwnedVolumes(user.UserId, work.Id, 18);

        MangaEntry favorite = Assert.Single(favoriteService.GetUserFavorites(user.UserId));
        Assert.True(favorite.IsFavorite);
        Assert.Equal(9, favorite.Rating);
        Assert.Equal(18, favorite.OwnedVolumes);

        favoriteService.SetRating(user.UserId, work.Id, null);
        Assert.Null(favRepo.Get(user.UserId, work.Id)!.Rating);

        favoriteService.SetFavorite(user.UserId, work.Id, false);
        Assert.Empty(favoriteService.GetUserFavorites(user.UserId));

        mangaService.Delete(work.Id);
        Assert.Empty(favoriteService.GetUserLibrary(user.UserId));

        using SqliteConnection connection = database.OpenConnection();
        using SqliteCommand checkWorkGenres = connection.CreateCommand();
        checkWorkGenres.CommandText = "SELECT COUNT(*) FROM WorkGenres WHERE WorkId = @workId;";
        checkWorkGenres.Parameters.AddWithValue("@workId", work.Id);
        Assert.Equal(0L, Convert.ToInt64(checkWorkGenres.ExecuteScalar()));
    }

    [Fact]
    public void SqliteFavoriteRepository_InvalidForeignKeys_ThrowsDomainNotFoundExceptions()
    {
        using SqliteDatabase database = SqliteDatabase.CreateInMemory();
        database.Initialize(seedGenres: false);

        var userRepo = new SqliteUserRepository(database);
        var mangaRepo = new SqliteMangaRepository(database);
        var favRepo = new SqliteFavoriteRepository(database);

        var user = User.CreateWithPassword("Guts", "berserk99", new Pbkdf2PasswordHasher());
        userRepo.Save(user);

        var manga = new Manga(0, "Berserk", "Kentaro Miura", 42, 376);
        mangaRepo.Save(manga);

        Assert.Throws<UserNotFoundException>(() =>
            favRepo.Save(new MangaEntry(999, manga.Id, 1)));

        Assert.Throws<MangaNotFoundException>(() =>
            favRepo.Save(new MangaEntry(user.UserId, 999, 1)));
    }

    [Fact]
    public void SqliteDatabase_FileBacked_PersistsAcrossReopenAndReleasesFileHandlesOnDispose()
    {
        string tempDir = CreateTempDirectory();
        string dbPath = SqliteDatabase.GetDefaultDatabasePath(tempDir);

        var expectedDate = new DateTime(2026, 5, 10, 14, 30, 0, DateTimeKind.Utc);
        int userId;
        int workId;

        using (var database = new SqliteDatabase(dbPath))
        {
            database.Initialize();

            var userService = new UserService(new SqliteUserRepository(database), new Pbkdf2PasswordHasher());
            var mangaService = new MangaService(new SqliteMangaRepository(database));
            var favRepo = new SqliteFavoriteRepository(database);

            User user = userService.Register(new CreateUserDto("Frieren", "magic1000"));
            Manga work = mangaService.Create(new CreateMangaDto(
                "Sousou no Frieren",
                "Kanehito Yamada",
                VolumeCount: 13,
                FinalChapters: 140,
                Status: MangaStatus.Ongoing,
                Type: MediaType.Manga,
                Illustrator: "Tsukasa Abe",
                CoverPath: "covers/frieren.png",
                Synopsis: "El viaje tras derrotar al Rey Demonio.",
                Genres: ["Fantasía", "Aventura"]));

            userId = user.UserId;
            workId = work.Id;

            favRepo.Save(new MangaEntry(
                userId,
                workId,
                currentChapter: 88,
                status: ReadingStatus.Reading,
                ownedVolumes: 11,
                rating: 10,
                isFavorite: true,
                dateAdded: expectedDate));
        }

        Assert.True(File.Exists(dbPath));

        using (var reopened = new SqliteDatabase(dbPath))
        {
            reopened.Initialize();

            var userService = new UserService(new SqliteUserRepository(reopened), new Pbkdf2PasswordHasher());
            var mangaService = new MangaService(new SqliteMangaRepository(reopened));
            var favRepo = new SqliteFavoriteRepository(reopened);

            Session session = userService.Authenticate("frieren", "magic1000");
            Assert.Equal(userId, session.UserId);

            Manga persistedWork = mangaService.GetById(workId);
            Assert.Equal("Tsukasa Abe", persistedWork.Illustrator);
            Assert.Equal(2, persistedWork.Genres.Count);

            MangaEntry? entry = favRepo.Get(userId, workId);
            Assert.NotNull(entry);
            Assert.Equal(expectedDate, entry.DateAdded);
            Assert.Equal(DateTimeKind.Utc, entry.DateAdded.Kind);
            Assert.True(entry.IsFavorite);
            Assert.Equal(10, entry.Rating);
        }

        Directory.Delete(tempDir, recursive: true);
        Assert.False(Directory.Exists(tempDir));
    }

    [Fact]
    public void JsonToSqliteImporter_MigratesAllEntitiesAndIsIdempotent()
    {
        string jsonDir = CreateTempDirectory();
        var hasher = new Pbkdf2PasswordHasher();

        var jsonUsers = new JsonUserRepository(jsonDir);
        var jsonMangas = new JsonMangaRepository(jsonDir);
        var jsonFavorites = new JsonFavoriteRepository(jsonDir);

        User jsonUser = User.CreateWithPassword("Lelouch", "geass123", hasher, Role.Admin);
        jsonUsers.Save(jsonUser);

        var jsonWork = new Manga(
            id: 0,
            title: "Spice and Wolf",
            mangaka: "Isuna Hasekura",
            volumeCount: 24,
            finalChapters: 0,
            status: MangaStatus.Ongoing,
            type: MediaType.LightNovel,
            illustrator: "Jyuu Ayakura",
            synopsis: "Comercio medieval y Holo.");
        jsonWork.AddGenre(new Genre("Aventura"));
        jsonWork.AddGenre(new Genre("Romance"));
        jsonMangas.Save(jsonWork);

        var entryDate = new DateTime(2026, 2, 1, 9, 15, 0, DateTimeKind.Utc);
        jsonFavorites.Save(new MangaEntry(
            jsonUser.UserId,
            jsonWork.Id,
            currentChapter: 5,
            status: ReadingStatus.Reading,
            ownedVolumes: 6,
            rating: 10,
            isFavorite: true,
            dateAdded: entryDate));

        Assert.True(JsonToSqliteImporter.HasJsonDataFiles(jsonDir));

        using SqliteDatabase database = SqliteDatabase.CreateInMemory();
        database.Initialize();

        var importer = new JsonToSqliteImporter(database);
        JsonImportResult firstRun = importer.Import(jsonDir);

        Assert.Equal(1, firstRun.UsersImported);
        Assert.Equal(0, firstRun.UsersSkipped);
        Assert.Equal(1, firstRun.WorksImported);
        Assert.Equal(0, firstRun.WorksSkipped);
        Assert.Equal(1, firstRun.EntriesImported);
        Assert.Equal(0, firstRun.EntriesSkipped);
        Assert.True(firstRun.HasImportedAny);

        var userService = new UserService(new SqliteUserRepository(database), hasher);
        Session session = userService.Authenticate("lelouch", "geass123");
        Assert.Equal(Role.Admin, session.Role);

        Manga persistedWork = Assert.Single(new SqliteMangaRepository(database).GetAll());
        Assert.Equal("Spice and Wolf", persistedWork.Title);
        Assert.Equal(MediaType.LightNovel, persistedWork.Type);
        Assert.Equal("Jyuu Ayakura", persistedWork.Illustrator);
        Assert.Equal(2, persistedWork.Genres.Count);

        MangaEntry? persistedEntry = new SqliteFavoriteRepository(database).Get(session.UserId, persistedWork.Id);
        Assert.NotNull(persistedEntry);
        Assert.True(persistedEntry.IsFavorite);
        Assert.Equal(10, persistedEntry.Rating);
        Assert.Equal(6, persistedEntry.OwnedVolumes);
        Assert.Equal(entryDate, persistedEntry.DateAdded);

        JsonImportResult secondRun = importer.Import(jsonDir);
        Assert.Equal(0, secondRun.UsersImported);
        Assert.Equal(1, secondRun.UsersSkipped);
        Assert.Equal(0, secondRun.WorksImported);
        Assert.Equal(1, secondRun.WorksSkipped);
        Assert.Equal(0, secondRun.EntriesImported);
        Assert.Equal(1, secondRun.EntriesSkipped);
        Assert.False(secondRun.HasImportedAny);
    }

    [Fact]
    public void JsonToSqliteImporter_NoJsonFiles_ReturnsZeroCounts()
    {
        string emptyDir = CreateTempDirectory();
        Assert.False(JsonToSqliteImporter.HasJsonDataFiles(emptyDir));

        using SqliteDatabase database = SqliteDatabase.CreateInMemory();
        database.Initialize(seedGenres: false);

        JsonImportResult result = new JsonToSqliteImporter(database).Import(emptyDir);
        Assert.Equal(0, result.TotalProcessed);
        Assert.False(result.HasImportedAny);
    }
}
