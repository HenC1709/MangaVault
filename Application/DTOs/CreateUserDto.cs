using MangaVault.Core.Enums;

namespace MangaVault.Application.DTOs;

public sealed record CreateUserDto(
    string Name,
    string Password,
    Role Role = Role.User);
