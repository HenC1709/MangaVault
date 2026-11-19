using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Infrastructure.Persistence.Json;
using Xunit;

namespace MangaVault.Tests;

public sealed class MangaServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"mangavault-tests-{Guid.NewGuid():N}");
    private readonly MangaService _service;

    public MangaServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        _service = new MangaService(new JsonMangaRepository(_tempDir));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Create_AllowsSameTitleAcrossMangaAndLightNovel()
    {
        Manga manga = _service.Create(new CreateMangaDto(
            "Overlord",
            "Kugane Maruyama",
            VolumeCount: 19,
            FinalChapters: 78,
            Type: MediaType.Manga,
            Genres: ["Fantasía", "Isekai"]));

        Manga novel = _service.Create(new CreateMangaDto(
            "Overlord",
            "Kugane Maruyama",
            VolumeCount: 16,
            FinalChapters: 0,
            Type: MediaType.LightNovel,
            Illustrator: "so-bin",
            Genres: ["Fantasía", "Isekai"]));

        Assert.Equal(1, manga.Id);
        Assert.Equal(2, novel.Id);

        Manga persistedNovel = _service.GetById(novel.Id);
        Assert.Equal(MediaType.LightNovel, persistedNovel.Type);
        Assert.Equal("so-bin", persistedNovel.Illustrator);
        Assert.Equal(2, persistedNovel.Genres.Count);
    }

    [Fact]
    public void Create_DuplicateTitleAndSameType_ThrowsDuplicateMangaException()
    {
        _service.Create(new CreateMangaDto("Berserk", "Kentaro Miura", Type: MediaType.Manga));

        Assert.Throws<DuplicateMangaException>(() =>
            _service.Create(new CreateMangaDto("berserk", "Kentaro Miura", Type: MediaType.Manga)));
    }

    [Fact]
    public void SearchByTitle_FiltersByQueryAndType()
    {
        _service.Create(new CreateMangaDto("Re:Zero", "Tappei Nagatsuki", Type: MediaType.LightNovel));
        _service.Create(new CreateMangaDto("Re:Zero", "Daichi Matsuse", Type: MediaType.Manga));
        _service.Create(new CreateMangaDto("Vagabond", "Takehiko Inoue", Type: MediaType.Manga));

        IReadOnlyList<Manga> onlyNovels = _service.SearchByTitle("zero", MediaType.LightNovel);

        Assert.Single(onlyNovels);
        Assert.Equal(MediaType.LightNovel, onlyNovels[0].Type);
    }

    [Fact]
    public void UpdateTitle_ChangesTitleAndPersists()
    {
        Manga created = _service.Create(new CreateMangaDto("Mushoku", "Rifujin", Type: MediaType.LightNovel));

        Manga updated = _service.UpdateTitle(created.Id, "Mushoku Tensei");

        Assert.Equal("Mushoku Tensei", updated.Title);
        Assert.Equal("Mushoku Tensei", _service.GetById(created.Id).Title);
    }

    [Fact]
    public void Delete_NonExistentId_ThrowsMangaNotFoundException()
    {
        Assert.Throws<MangaNotFoundException>(() => _service.Delete(999));
    }
}
