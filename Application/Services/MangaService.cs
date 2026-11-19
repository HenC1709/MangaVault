using MangaVault.Application.DTOs;
using MangaVault.Application.Mappers;
using MangaVault.Application.Validators;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;

namespace MangaVault.Application.Services;

public class MangaService(IMangaRepository mangaRepository)
{
    public Manga Create(CreateMangaDto dto)
    {
        MangaValidator.ValidateCreate(dto);

        Manga manga = MangaMapper.ToEntity(dto);
        mangaRepository.Save(manga);
        return manga;
    }

    public IReadOnlyList<Manga> GetAll() => mangaRepository.GetAll();

    public Manga GetById(int id) =>
        mangaRepository.GetById(id) ?? throw new MangaNotFoundException(id);

    public IReadOnlyList<Manga> SearchByTitle(string query, MediaType? type = null)
    {
        string trimmed = (query ?? "").Trim();
        IEnumerable<Manga> works = mangaRepository.GetAll();

        if (type.HasValue)
            works = works.Where(m => m.Type == type.Value);

        if (!string.IsNullOrEmpty(trimmed))
            works = works.Where(m => m.Title.Contains(trimmed, StringComparison.OrdinalIgnoreCase));

        return works.ToList();
    }

    public Manga UpdateTitle(int id, string newTitle)
    {
        MangaValidator.ValidateTitle(newTitle, nameof(newTitle));

        Manga manga = GetById(id);
        manga.SetTitle(newTitle);
        mangaRepository.Update(manga);
        return manga;
    }

    public Manga Update(int id, CreateMangaDto dto)
    {
        MangaValidator.ValidateCreate(dto);

        Manga manga = GetById(id);
        manga.UpdateDetails(
            dto.Title,
            dto.Mangaka,
            dto.VolumeCount,
            dto.FinalChapters,
            dto.Status,
            dto.Type,
            dto.Illustrator,
            dto.CoverPath,
            dto.Synopsis);

        if (dto.Genres is not null)
        {
            manga.Genres.Clear();
            MangaMapper.AddGenres(manga, dto.Genres);
        }

        mangaRepository.Update(manga);
        return manga;
    }

    public void Delete(int id) => mangaRepository.Delete(id);
}
