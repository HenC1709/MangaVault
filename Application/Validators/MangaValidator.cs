using MangaVault.Application.DTOs;
using MangaVault.Core.Constants;

namespace MangaVault.Application.Validators;

public static class MangaValidator
{
    public static void ValidateCreate(CreateMangaDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        ValidateTitle(dto.Title, nameof(dto));

        if ((dto.Mangaka ?? "").Trim().Length > ValidationRules.MaxAuthorLength)
            throw new ArgumentException(ErrorMessages.AuthorTooLong, nameof(dto));

        if (dto.VolumeCount < 0)
            throw new ArgumentOutOfRangeException(nameof(dto), ErrorMessages.NegativeVolumes);

        if (dto.FinalChapters < 0)
            throw new ArgumentOutOfRangeException(nameof(dto), ErrorMessages.NegativeChapters);

        if (!Enum.IsDefined(dto.Status))
            throw new ArgumentOutOfRangeException(nameof(dto), ErrorMessages.InvalidMangaStatus);

        if (!Enum.IsDefined(dto.Type))
            throw new ArgumentOutOfRangeException(nameof(dto), ErrorMessages.InvalidMediaType);
    }

    public static void ValidateTitle(string title, string paramName = "title")
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(ErrorMessages.TitleRequired, paramName);

        if (title.Trim().Length > ValidationRules.MaxTitleLength)
            throw new ArgumentException(ErrorMessages.TitleTooLong, paramName);
    }
}
