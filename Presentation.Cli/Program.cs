using MangaVault.Application.Services;
using MangaVault.Core.Interfaces;
using MangaVault.Infrastructure.Logging;
using MangaVault.Infrastructure.Persistence.Sqlite;
using MangaVault.Infrastructure.Security;
using MangaVault.Presentation.Cli.Menus;

string? customDataDir = Environment.GetEnvironmentVariable("MANGAVAULT_DATA_DIR");
string dataDirectory = !string.IsNullOrWhiteSpace(customDataDir)
    ? customDataDir.Trim()
    : Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MangaVault");

string databasePath = SqliteDatabase.GetDefaultDatabasePath(dataDirectory);
using var database = new SqliteDatabase(databasePath);
database.Initialize();

IUserRepository userRepository = new SqliteUserRepository(database);
IMangaRepository mangaRepository = new SqliteMangaRepository(database);
IFavoriteRepository favoriteRepository = new SqliteFavoriteRepository(database);
IGenreRepository genreRepository = new SqliteGenreRepository(database);
var importer = new JsonToSqliteImporter(database);

IPasswordHasher passwordHasher = new Pbkdf2PasswordHasher();
var logger = new ConsoleLogger();

var userService = new UserService(userRepository, passwordHasher);
var mangaService = new MangaService(mangaRepository);
var favoriteService = new FavoriteService(favoriteRepository, userRepository, mangaRepository);

var userMenu = new UserMenu(userService, logger);
var mangaMenu = new MangaMenu(mangaService, favoriteService, logger, genreRepository);
var mainMenu = new MainMenu(userMenu, mangaMenu, dataDirectory, databasePath, importer, logger);

mainMenu.Run();
