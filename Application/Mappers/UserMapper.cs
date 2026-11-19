using MangaVault.Application.DTOs;
using MangaVault.Core.Entities;
using MangaVault.Core.Interfaces;

namespace MangaVault.Application.Mappers;

public static class UserMapper
{
    public static User ToEntity(CreateUserDto dto, IPasswordHasher passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(passwordHasher);

        return User.CreateWithPassword(dto.Name, dto.Password, passwordHasher, dto.Role);
    }
}
