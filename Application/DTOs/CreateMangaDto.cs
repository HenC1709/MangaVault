using MangaVault.Core.Enums;

namespace MangaVault.Application.DTOs;

public sealed record CreateMangaDto(
    string Title,
    string Mangaka = "",
    int VolumeCount = 0,
    int FinalChapters = 0,
    MangaStatus Status = MangaStatus.Ongoing,
    MediaType Type = MediaType.Manga,
    string? Illustrator = null,
    string? CoverPath = null,
    string? Synopsis = null,
    IReadOnlyList<string>? Genres = null);
