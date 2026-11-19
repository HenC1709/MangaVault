using MangaVault.Application.DTOs;
using MangaVault.Core.Constants;

namespace MangaVault.Application.Validators;

public static class UserValidator
{
    public static void ValidateCreate(CreateUserDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException(ErrorMessages.UsernameRequired, nameof(dto));

        string trimmedName = dto.Name.Trim();
        if (trimmedName.Length is < ValidationRules.MinUsernameLength or > ValidationRules.MaxUsernameLength)
            throw new ArgumentException(ErrorMessages.UsernameLengthInvalid, nameof(dto));

        ValidatePassword(dto.Password, nameof(dto));

        if (!Enum.IsDefined(dto.Role))
            throw new ArgumentOutOfRangeException(nameof(dto), ErrorMessages.InvalidRole);
    }

    public static void ValidatePassword(string password, string paramName = "password")
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException(ErrorMessages.PasswordRequired, paramName);

        if (password.Length is < ValidationRules.MinPasswordLength or > ValidationRules.MaxPasswordLength)
            throw new ArgumentException(ErrorMessages.PasswordLengthInvalid, paramName);
    }
}
