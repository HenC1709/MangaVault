using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;
using MangaVault.Infrastructure.Logging;

namespace MangaVault.Presentation.Cli.Menus;

public sealed class MangaMenu(
    MangaService mangaService,
    FavoriteService favoriteService,
    ConsoleLogger logger,
    IGenreRepository? genreRepository = null)
{
    public void Show(Session? currentSession)
    {
        bool back = false;
        while (!back)
        {
            Console.WriteLine();
            Console.WriteLine("=== Catálogo y Biblioteca ===");
            Console.WriteLine("1. Listar catálogo");
            Console.WriteLine("2. Buscar obra por título");
            Console.WriteLine("3. Agregar obra");
            Console.WriteLine("4. Editar título de obra");
            Console.WriteLine("5. Eliminar obra");

            if (currentSession is not null)
            {
                Console.WriteLine($"--- Biblioteca de {currentSession.UserName} ---");
                Console.WriteLine("6. Ver mi biblioteca");
                Console.WriteLine("7. Ver mis favoritos");
                Console.WriteLine("8. Agregar obra a mi biblioteca");
                Console.WriteLine("9. Actualizar progreso de lectura");
                Console.WriteLine("10. Marcar / desmarcar favorito");
                Console.WriteLine("11. Calificar obra (1-10)");
                Console.WriteLine("12. Actualizar tomos poseídos");
            }
            else
            {
                Console.WriteLine("(Inicia sesión en 'Cuenta y Sesión' para gestionar tu biblioteca y favoritos)");
            }

            Console.WriteLine("0. Volver");
            Console.Write("Opción: ");

            string? option = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (option)
            {
                case "1":
                    ListWorks(mangaService.GetAll());
                    break;
                case "2":
                    SearchWorks();
                    break;
                case "3":
                    AddWork();
                    break;
                case "4":
                    EditWorkTitle();
                    break;
                case "5":
                    DeleteWork();
                    break;
                case "6" when currentSession is not null:
                    ShowLibrary(currentSession.UserId, onlyFavorites: false);
                    break;
                case "7" when currentSession is not null:
                    ShowLibrary(currentSession.UserId, onlyFavorites: true);
                    break;
                case "8" when currentSession is not null:
                    AddToUserLibrary(currentSession.UserId);
                    break;
                case "9" when currentSession is not null:
                    UpdateUserProgress(currentSession.UserId);
                    break;
                case "10" when currentSession is not null:
                    ToggleFavorite(currentSession.UserId);
                    break;
                case "11" when currentSession is not null:
                    RateWork(currentSession.UserId);
                    break;
                case "12" when currentSession is not null:
                    UpdateOwnedVolumes(currentSession.UserId);
                    break;
                case "0":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }
    }

    private static void ListWorks(IReadOnlyList<Manga> works)
    {
        if (works.Count == 0)
        {
            Console.WriteLine("No hay obras registradas.");
            return;
        }

        foreach (var m in works)
        {
            string typeTag = m.Type == MediaType.LightNovel ? "LN" : "Manga";
            string illustrator = string.IsNullOrWhiteSpace(m.Illustrator) ? "" : $" | Ilustrador: {m.Illustrator}";
            string genres = m.Genres.Count > 0
                ? $" | Géneros: {string.Join(", ", m.Genres.Select(g => g.Name))}"
                : "";
            Console.WriteLine($"[{m.Id}] [{typeTag}] {m.Title} — {m.Mangaka}{illustrator} | Tomos: {m.VolumeCount} | Caps: {m.FinalChapters} | Estado: {m.Status}{genres}");
        }
    }

    private void SearchWorks()
    {
        Console.Write("Texto del título (vacío = todos): ");
        string query = Console.ReadLine() ?? "";

        Console.Write("Filtrar por tipo (Enter=Todos, 0=Manga, 1=LN): ");
        string? rawType = Console.ReadLine()?.Trim();
        MediaType? type = int.TryParse(rawType, out int t) && Enum.IsDefined((MediaType)t)
            ? (MediaType)t
            : null;

        ListWorks(mangaService.SearchByTitle(query, type));
    }

    private void AddWork()
    {
        Console.WriteLine("Tipo: 0=Manga, 1=Novela Ligera");
        int typeInput = ReadNonNegativeInt("Tipo (por defecto 0): ");
        MediaType mediaType = Enum.IsDefined((MediaType)typeInput) ? (MediaType)typeInput : MediaType.Manga;

        Console.Write("Título: ");
        string title = Console.ReadLine() ?? "";

        Console.Write(mediaType == MediaType.LightNovel ? "Autor / Escritor: " : "Autor / Mangaka: ");
        string author = Console.ReadLine() ?? "";

        string? illustrator = null;
        if (mediaType == MediaType.LightNovel)
        {
            Console.Write("Ilustrador (opcional): ");
            illustrator = Console.ReadLine();
        }

        int volumes = ReadNonNegativeInt("Cantidad de tomos (0 si no aplica): ");
        int chapters = ReadNonNegativeInt("Cantidad de capítulos (0 si no aplica): ");

        Console.WriteLine("Estado: 0=Ongoing, 1=Completed, 2=Hiatus, 3=Cancelled");
        int statusInput = ReadNonNegativeInt("Estado (por defecto 0): ");
        MangaStatus status = Enum.IsDefined((MangaStatus)statusInput) ? (MangaStatus)statusInput : MangaStatus.Ongoing;

        if (genreRepository is not null)
        {
            List<Genre> availableGenres = genreRepository.GetAll();
            if (availableGenres.Count > 0)
            {
                Console.WriteLine($"Géneros disponibles: {string.Join(", ", availableGenres.Select(g => g.Name))}");
            }
        }

        Console.Write("Géneros (opcional, separados por coma): ");
        string? genresInput = Console.ReadLine();
        string[]? genres = string.IsNullOrWhiteSpace(genresInput)
            ? null
            : genresInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        try
        {
            var dto = new CreateMangaDto(title, author, volumes, chapters, status, mediaType, illustrator, Genres: genres);
            Manga created = mangaService.Create(dto);
            logger.Info($"Obra guardada: [{created.Id}] {created.Title} ({created.Type}).");
            Console.WriteLine($"Obra '{created.Title}' ({created.Type}) guardada con Id {created.Id}.");
        }
        catch (Exception ex) when (ex is DuplicateMangaException or ArgumentException)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void EditWorkTitle()
    {
        if (!TryReadPositiveInt("Id de la obra a editar: ", out int id))
            return;

        Console.Write("Nuevo título: ");
        string newTitle = Console.ReadLine() ?? "";

        try
        {
            Manga updated = mangaService.UpdateTitle(id, newTitle);
            logger.Info($"Título actualizado para obra {updated.Id}: '{updated.Title}'.");
            Console.WriteLine($"Título actualizado a '{updated.Title}'.");
        }
        catch (Exception ex) when (ex is MangaNotFoundException or DuplicateMangaException or ArgumentException)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void DeleteWork()
    {
        if (!TryReadPositiveInt("Id de la obra a eliminar: ", out int id))
            return;

        try
        {
            mangaService.Delete(id);
            logger.Info($"Obra eliminada: Id {id}.");
            Console.WriteLine($"Obra con Id {id} eliminada.");
        }
        catch (MangaNotFoundException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void ShowLibrary(int userId, bool onlyFavorites)
    {
        IReadOnlyList<MangaEntry> entries = onlyFavorites
            ? favoriteService.GetUserFavorites(userId)
            : favoriteService.GetUserLibrary(userId);

        if (entries.Count == 0)
        {
            Console.WriteLine(onlyFavorites ? "No tienes favoritos registrados." : "Tu biblioteca está vacía.");
            return;
        }

        var worksById = mangaService.GetAll().ToDictionary(m => m.Id);
        foreach (var e in entries)
        {
            string title = worksById.TryGetValue(e.MangaId, out var work) ? work.Title : $"Obra #{e.MangaId}";
            string fav = e.IsFavorite ? " ★" : "";
            string rating = e.Rating.HasValue ? $" | Nota: {e.Rating}/10" : "";
            Console.WriteLine($"[{e.MangaId}] {title}{fav} | Estado: {e.ReadingStatus} | Cap: {e.CurrentChapter} | Tomos: {e.OwnedVolumes}{rating}");
        }
    }

    private void AddToUserLibrary(int userId)
    {
        if (!TryReadPositiveInt("Id de la obra: ", out int mangaId))
            return;

        Console.WriteLine("Estado: 1=Reading, 2=PlanToRead, 3=Completed, 4=OnHold, 5=Dropped");
        int statusVal = ReadNonNegativeInt("Estado (por defecto 2): ");
        ReadingStatus status = Enum.IsDefined((ReadingStatus)statusVal) ? (ReadingStatus)statusVal : ReadingStatus.PlanToRead;

        int chapter = ReadNonNegativeInt("Capítulo actual (por defecto 0): ");
        Console.Write("¿Marcar como favorito? (s/N): ");
        bool isFav = string.Equals(Console.ReadLine()?.Trim(), "s", StringComparison.OrdinalIgnoreCase);

        try
        {
            favoriteService.AddToLibrary(userId, mangaId, status, chapter, isFav);
            Console.WriteLine("Obra agregada a tu biblioteca.");
        }
        catch (Exception ex) when (ex is DuplicateFavoriteException or MangaNotFoundException or UserNotFoundException or ArgumentOutOfRangeException)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void UpdateUserProgress(int userId)
    {
        if (!TryReadPositiveInt("Id de la obra: ", out int mangaId))
            return;

        int newChapter = ReadNonNegativeInt("Nuevo capítulo actual: ");
        Console.WriteLine("Estado: 1=Reading, 2=PlanToRead, 3=Completed, 4=OnHold, 5=Dropped");
        int statusVal = ReadNonNegativeInt("Nuevo estado (por defecto 1): ");
        ReadingStatus status = Enum.IsDefined((ReadingStatus)statusVal) ? (ReadingStatus)statusVal : ReadingStatus.Reading;

        try
        {
            MangaEntry entry = favoriteService.UpdateProgress(userId, mangaId, newChapter, status);
            Console.WriteLine($"Progreso actualizado: Cap. {entry.CurrentChapter} ({entry.ReadingStatus}).");
        }
        catch (Exception ex) when (ex is KeyNotFoundException or MangaNotFoundException or ArgumentOutOfRangeException)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void ToggleFavorite(int userId)
    {
        if (!TryReadPositiveInt("Id de la obra: ", out int mangaId))
            return;

        Console.Write("¿Marcar como favorito? (s/n): ");
        bool isFav = string.Equals(Console.ReadLine()?.Trim(), "s", StringComparison.OrdinalIgnoreCase);

        try
        {
            MangaEntry entry = favoriteService.SetFavorite(userId, mangaId, isFav);
            Console.WriteLine(entry.IsFavorite ? "Obra marcada como favorita." : "Obra quitada de favoritos.");
        }
        catch (Exception ex) when (ex is MangaNotFoundException or UserNotFoundException)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void RateWork(int userId)
    {
        if (!TryReadPositiveInt("Id de la obra: ", out int mangaId))
            return;

        Console.Write("Calificación (1-10, vacío para quitar): ");
        string? raw = Console.ReadLine()?.Trim();
        int? rating = string.IsNullOrEmpty(raw) ? null : int.TryParse(raw, out int r) ? r : -1;

        try
        {
            MangaEntry entry = favoriteService.SetRating(userId, mangaId, rating);
            Console.WriteLine(entry.Rating.HasValue
                ? $"Calificación guardada: {entry.Rating}/10."
                : "Calificación eliminada.");
        }
        catch (Exception ex) when (ex is KeyNotFoundException or ArgumentOutOfRangeException)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void UpdateOwnedVolumes(int userId)
    {
        if (!TryReadPositiveInt("Id de la obra: ", out int mangaId))
            return;

        int owned = ReadNonNegativeInt("Cantidad de tomos poseídos: ");

        try
        {
            MangaEntry entry = favoriteService.SetOwnedVolumes(userId, mangaId, owned);
            Console.WriteLine($"Tomos poseídos actualizados a {entry.OwnedVolumes}.");
        }
        catch (Exception ex) when (ex is KeyNotFoundException or ArgumentOutOfRangeException)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static bool TryReadPositiveInt(string prompt, out int value)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine()?.Trim(), out value) && value > 0)
            return true;

        Console.WriteLine("Id no válido.");
        return false;
    }

    private static int ReadNonNegativeInt(string prompt)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine()?.Trim();
        return int.TryParse(input, out int value) && value >= 0 ? value : 0;
    }
}
