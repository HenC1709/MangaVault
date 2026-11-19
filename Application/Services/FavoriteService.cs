using MangaVault.Core.Constants;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;

namespace MangaVault.Application.Services;

public class FavoriteService(
    IFavoriteRepository favoriteRepository,
    IUserRepository userRepository,
    IMangaRepository mangaRepository)
{
    public MangaEntry AddToLibrary(
        int userId,
        int mangaId,
        ReadingStatus status = ReadingStatus.PlanToRead,
        int currentChapter = 0,
        bool isFavorite = false)
    {
        EnsureUserExists(userId);
        Manga manga = EnsureMangaExists(mangaId);

        if (manga.FinalChapters > 0 && currentChapter > manga.FinalChapters)
            throw new ArgumentOutOfRangeException(nameof(currentChapter), ErrorMessages.ChapterExceedsTotal);

        var entry = new MangaEntry(userId, mangaId, currentChapter, status, isFavorite: isFavorite);
        favoriteRepository.Save(entry);
        return entry;
    }

    public MangaEntry UpdateProgress(int userId, int mangaId, int newChapter, ReadingStatus newStatus)
    {
        Manga manga = EnsureMangaExists(mangaId);
        MangaEntry entry = GetEntryOrThrow(userId, mangaId);

        int? maxChapters = manga.FinalChapters > 0 ? manga.FinalChapters : null;
        entry.UpdateProgress(newChapter, newStatus, maxChapters);
        favoriteRepository.Update(entry);
        return entry;
    }

    public MangaEntry SetFavorite(int userId, int mangaId, bool isFavorite)
    {
        EnsureUserExists(userId);
        EnsureMangaExists(mangaId);

        MangaEntry? existing = favoriteRepository.Get(userId, mangaId);
        if (existing is null)
        {
            var created = new MangaEntry(userId, mangaId, 0, ReadingStatus.PlanToRead, isFavorite: isFavorite);
            favoriteRepository.Save(created);
            return created;
        }

        existing.SetFavorite(isFavorite);
        favoriteRepository.Update(existing);
        return existing;
    }

    public MangaEntry SetRating(int userId, int mangaId, int? rating)
    {
        MangaEntry entry = GetEntryOrThrow(userId, mangaId);
        entry.SetRating(rating);
        favoriteRepository.Update(entry);
        return entry;
    }

    public MangaEntry SetOwnedVolumes(int userId, int mangaId, int ownedVolumes)
    {
        MangaEntry entry = GetEntryOrThrow(userId, mangaId);
        entry.SetOwnedVolumes(ownedVolumes);
        favoriteRepository.Update(entry);
        return entry;
    }

    public IReadOnlyList<MangaEntry> GetUserLibrary(int userId)
    {
        EnsureUserExists(userId);
        return favoriteRepository.GetByUser(userId);
    }

    public IReadOnlyList<MangaEntry> GetUserFavorites(int userId) =>
        GetUserLibrary(userId).Where(e => e.IsFavorite).ToList();

    public void RemoveFromLibrary(int userId, int mangaId) =>
        favoriteRepository.Delete(userId, mangaId);

    private void EnsureUserExists(int userId)
    {
        if (userRepository.GetById(userId) is null)
            throw new UserNotFoundException(userId);
    }

    private Manga EnsureMangaExists(int mangaId) =>
        mangaRepository.GetById(mangaId) ?? throw new MangaNotFoundException(mangaId);

    private MangaEntry GetEntryOrThrow(int userId, int mangaId) =>
        favoriteRepository.Get(userId, mangaId) ?? throw new KeyNotFoundException(ErrorMessages.EntryNotFound);
}
