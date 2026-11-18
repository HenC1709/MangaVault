using MangaVault.Core.Entities;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;

namespace MangaVault.Infrastructure.Persistence.Json;

public class JsonMangaRepository(string? storageDirectory = null) : IMangaRepository
{
    private readonly JsonFileStore<Manga> _store = new("mangas.json", storageDirectory);

    public List<Manga> GetAll() => _store.ReadAll();

    public Manga? GetById(int id) =>
        _store.Read(mangas => mangas.FirstOrDefault(m => m.Id == id));

    public void Save(Manga manga)
    {
        ArgumentNullException.ThrowIfNull(manga);

        _store.Mutate(mangas =>
        {
            if (manga.Id <= 0)
            {
                manga.Id = mangas.Count == 0 ? 1 : mangas.Max(m => m.Id) + 1;
            }

            if (mangas.Any(m => m.Id == manga.Id))
                throw new DuplicateMangaException(manga.Id);

            if (mangas.Any(m => m.Type == manga.Type && string.Equals(m.Title, manga.Title, StringComparison.OrdinalIgnoreCase)))
                throw new DuplicateMangaException(manga.Title, manga.Type);

            mangas.Add(manga);
        });
    }

    public void Update(Manga manga)
    {
        ArgumentNullException.ThrowIfNull(manga);

        _store.Mutate(mangas =>
        {
            int index = mangas.FindIndex(m => m.Id == manga.Id);
            if (index == -1)
                throw new MangaNotFoundException(manga.Id);

            if (mangas.Any(m => m.Id != manga.Id && m.Type == manga.Type && string.Equals(m.Title, manga.Title, StringComparison.OrdinalIgnoreCase)))
                throw new DuplicateMangaException(manga.Title, manga.Type);

            mangas[index] = manga;
        });
    }

    public void Delete(int id)
    {
        _store.Mutate(mangas =>
        {
            int removed = mangas.RemoveAll(m => m.Id == id);
            if (removed == 0)
                throw new MangaNotFoundException(id);
        });
    }
}
