using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;
using MangaVault.Infrastructure.Persistence.Json;

IMangaRepository mangaRepo = new JsonMangaRepository();
string dataDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "MangaVault");

bool running = true;
while (running)
{
    Console.WriteLine();
    Console.WriteLine("=== MangaVault (MVP 1) ===");
    Console.WriteLine("1. Listar obras");
    Console.WriteLine("2. Agregar obra");
    Console.WriteLine("3. Editar título de obra");
    Console.WriteLine("4. Eliminar obra");
    Console.WriteLine("5. Ver carpeta de datos");
    Console.WriteLine("0. Salir");
    Console.Write("Opción: ");

    string? option = Console.ReadLine()?.Trim();
    Console.WriteLine();

    switch (option)
    {
        case "1":
            ListWorks(mangaRepo);
            break;
        case "2":
            AddWork(mangaRepo);
            break;
        case "3":
            EditWorkTitle(mangaRepo);
            break;
        case "4":
            DeleteWork(mangaRepo);
            break;
        case "5":
            Console.WriteLine($"Carpeta de datos: {dataDir}");
            break;
        case "0":
            running = false;
            Console.WriteLine("Hasta luego.");
            break;
        default:
            Console.WriteLine("Opción no válida.");
            break;
    }
}

static void ListWorks(IMangaRepository repo)
{
    var works = repo.GetAll();
    if (works.Count == 0)
    {
        Console.WriteLine("No hay obras registradas todavía.");
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

static void AddWork(IMangaRepository repo)
{
    Console.WriteLine("Tipo: 0=Manga, 1=Novela Ligera");
    int typeInput = ReadNonNegativeInt("Tipo (por defecto 0): ");
    MediaType mediaType = Enum.IsDefined(typeof(MediaType), typeInput)
        ? (MediaType)typeInput
        : MediaType.Manga;

    Console.Write("Título: ");
    string title = (Console.ReadLine() ?? "").Trim();
    if (string.IsNullOrWhiteSpace(title))
    {
        Console.WriteLine("El título no puede estar vacío.");
        return;
    }

    Console.Write(mediaType == MediaType.LightNovel ? "Autor / Escritor: " : "Autor / Mangaka: ");
    string author = (Console.ReadLine() ?? "").Trim();

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
    MangaStatus status = Enum.IsDefined(typeof(MangaStatus), statusInput)
        ? (MangaStatus)statusInput
        : MangaStatus.Ongoing;

    Console.Write("Géneros (opcional, separados por coma): ");
    string? genresInput = Console.ReadLine();

    try
    {
        var manga = new Manga(0, title, author, volumes, chapters, status, mediaType, illustrator);

        if (!string.IsNullOrWhiteSpace(genresInput))
        {
            foreach (string raw in genresInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                manga.AddGenre(new Genre(raw));
            }
        }

        repo.Save(manga);
        Console.WriteLine($"Obra '{manga.Title}' ({mediaType}) guardada con Id {manga.Id}.");
    }
    catch (DuplicateMangaException ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"Dato inválido: {ex.Message}");
    }
}

static void EditWorkTitle(IMangaRepository repo)
{
    Console.Write("Id de la obra a editar: ");
    if (!int.TryParse(Console.ReadLine()?.Trim(), out int id) || id <= 0)
    {
        Console.WriteLine("Id no válido.");
        return;
    }

    var manga = repo.GetById(id);
    if (manga is null)
    {
        Console.WriteLine($"No se encontró una obra con el Id {id}.");
        return;
    }

    Console.Write($"Nuevo título (actual '{manga.Title}'): ");
    string newTitle = (Console.ReadLine() ?? "").Trim();

    try
    {
        manga.SetTitle(newTitle);
        repo.Update(manga);
        Console.WriteLine($"Título actualizado a '{manga.Title}'.");
    }
    catch (MangaNotFoundException ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
    catch (DuplicateMangaException ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"Dato inválido: {ex.Message}");
    }
}

static void DeleteWork(IMangaRepository repo)
{
    Console.Write("Id de la obra a eliminar: ");
    if (!int.TryParse(Console.ReadLine()?.Trim(), out int id) || id <= 0)
    {
        Console.WriteLine("Id no válido.");
        return;
    }

    try
    {
        repo.Delete(id);
        Console.WriteLine($"Obra con Id {id} eliminada.");
    }
    catch (MangaNotFoundException ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static int ReadNonNegativeInt(string prompt)
{
    Console.Write(prompt);
    string? input = Console.ReadLine()?.Trim();
    return int.TryParse(input, out int value) && value >= 0 ? value : 0;
}
