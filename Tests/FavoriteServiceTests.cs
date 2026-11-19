using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Infrastructure.Persistence.Json;
using MangaVault.Infrastructure.Security;
using Xunit;

namespace MangaVault.Tests;

public sealed class FavoriteServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"mangavault-tests-{Guid.NewGuid():N}");
    private readonly UserService _userService;
    private readonly MangaService _mangaService;
    private readonly FavoriteService _favoriteService;

    public FavoriteServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        var userRepo = new JsonUserRepository(_tempDir);
        var mangaRepo = new JsonMangaRepository(_tempDir);
        var favRepo = new JsonFavoriteRepository(_tempDir);

        _userService = new UserService(userRepo, new Pbkdf2PasswordHasher());
        _mangaService = new MangaService(mangaRepo);
        _favoriteService = new FavoriteService(favRepo, userRepo, mangaRepo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void AddToLibrary_Duplicate_ThrowsDuplicateFavoriteException()
    {
        User user = _userService.Register(new CreateUserDto("Shinji", "eva01pass"));
        Manga manga = _mangaService.Create(new CreateMangaDto("Vinland Saga", "Makoto Yukimura", FinalChapters: 210));

        MangaEntry entry = _favoriteService.AddToLibrary(user.UserId, manga.Id, ReadingStatus.Reading, 10, isFavorite: true);
        Assert.True(entry.IsFavorite);

        Assert.Throws<DuplicateFavoriteException>(() =>
            _favoriteService.AddToLibrary(user.UserId, manga.Id));
    }

    [Fact]
    public void UpdateProgress_ExceedsFinalChapters_ThrowsArgumentOutOfRangeException()
    {
        User user = _userService.Register(new CreateUserDto("Lelouch", "geass123"));
        Manga manga = _mangaService.Create(new CreateMangaDto("Monster", "Naoki Urasawa", FinalChapters: 162));
        _favoriteService.AddToLibrary(user.UserId, manga.Id);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _favoriteService.UpdateProgress(user.UserId, manga.Id, 163, ReadingStatus.Reading));
    }

    [Fact]
    public void SetFavoriteRatingAndVolumes_PersistCorrectly()
    {
        User user = _userService.Register(new CreateUserDto("Subaru", "returnbydeath"));
        Manga m1 = _mangaService.Create(new CreateMangaDto("86", "Asato Asato", Type: MediaType.LightNovel));
        Manga m2 = _mangaService.Create(new CreateMangaDto("Kingdom", "Yasuhisa Hara", Type: MediaType.Manga));

        _favoriteService.SetFavorite(user.UserId, m1.Id, true);
        _favoriteService.SetRating(user.UserId, m1.Id, 10);
        _favoriteService.SetOwnedVolumes(user.UserId, m1.Id, 12);
        _favoriteService.AddToLibrary(user.UserId, m2.Id, ReadingStatus.PlanToRead, isFavorite: false);

        IReadOnlyList<MangaEntry> favorites = _favoriteService.GetUserFavorites(user.UserId);
        Assert.Single(favorites);
        Assert.Equal(10, favorites[0].Rating);
        Assert.Equal(12, favorites[0].OwnedVolumes);

        _favoriteService.RemoveFromLibrary(user.UserId, m1.Id);
        Assert.Empty(_favoriteService.GetUserFavorites(user.UserId));
    }
}
