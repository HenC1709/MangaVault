using MangaVault.Application.Services;
using MangaVault.Core.Interfaces;
using MangaVault.Infrastructure.Logging;
using MangaVault.Infrastructure.Persistence.Json;
using MangaVault.Infrastructure.Security;
using MangaVault.Presentation.Cli.Menus;

string dataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "MangaVault");

IUserRepository userRepository = new JsonUserRepository(dataDirectory);
IMangaRepository mangaRepository = new JsonMangaRepository(dataDirectory);
IFavoriteRepository favoriteRepository = new JsonFavoriteRepository(dataDirectory);
IPasswordHasher passwordHasher = new Pbkdf2PasswordHasher();
var logger = new ConsoleLogger();

var userService = new UserService(userRepository, passwordHasher);
var mangaService = new MangaService(mangaRepository);
var favoriteService = new FavoriteService(favoriteRepository, userRepository, mangaRepository);

var userMenu = new UserMenu(userService, logger);
var mangaMenu = new MangaMenu(mangaService, favoriteService, logger);
var mainMenu = new MainMenu(userMenu, mangaMenu, dataDirectory);

mainMenu.Run();
