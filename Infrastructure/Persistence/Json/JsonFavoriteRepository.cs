using MangaVault.Core.Constants;
using MangaVault.Core.Entities;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;

namespace MangaVault.Infrastructure.Persistence.Json;

public class JsonFavoriteRepository(string? storageDirectory = null) : IFavoriteRepository
{
    private readonly JsonFileStore<MangaEntry> _store = new("favorites.json", storageDirectory);

    public List<MangaEntry> GetAll() => _store.ReadAll();

    public List<MangaEntry> GetByUser(int userId) =>
        _store.Read(entries => entries.Where(e => e.UserId == userId).ToList());

    public MangaEntry? Get(int userId, int mangaId) =>
        _store.Read(entries => entries.FirstOrDefault(e => e.UserId == userId && e.MangaId == mangaId));

    public void Save(MangaEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _store.Mutate(entries =>
        {
            if (entries.Any(e => e.UserId == entry.UserId && e.MangaId == entry.MangaId))
                throw new DuplicateFavoriteException(entry.UserId, entry.MangaId);

            entries.Add(entry);
        });
    }

    public void Update(MangaEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _store.Mutate(entries =>
        {
            int index = entries.FindIndex(e => e.UserId == entry.UserId && e.MangaId == entry.MangaId);
            if (index == -1)
                throw new KeyNotFoundException(ErrorMessages.EntryNotFound);

            entries[index] = entry;
        });
    }

    public void Delete(int userId, int mangaId)
    {
        _store.Mutate(entries =>
        {
            int removed = entries.RemoveAll(e => e.UserId == userId && e.MangaId == mangaId);
            if (removed == 0)
                throw new KeyNotFoundException(ErrorMessages.EntryNotFound);
        });
    }
}
