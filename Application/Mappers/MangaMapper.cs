using MangaVault.Application.DTOs;
using MangaVault.Core.Entities;

namespace MangaVault.Application.Mappers;

public static class MangaMapper
{
    public static Manga ToEntity(CreateMangaDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var manga = new Manga(
            id: 0,
            title: dto.Title,
            mangaka: dto.Mangaka,
            volumeCount: dto.VolumeCount,
            finalChapters: dto.FinalChapters,
            status: dto.Status,
            type: dto.Type,
            illustrator: dto.Illustrator,
            coverPath: dto.CoverPath,
            synopsis: dto.Synopsis);

        AddGenres(manga, dto.Genres);
        return manga;
    }

    public static void AddGenres(Manga manga, IReadOnlyList<string>? genres)
    {
        ArgumentNullException.ThrowIfNull(manga);
        if (genres is null)
            return;

        foreach (string raw in genres)
        {
            if (!string.IsNullOrWhiteSpace(raw))
                manga.AddGenre(new Genre(raw));
        }
    }
}
