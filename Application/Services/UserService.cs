using MangaVault.Application.DTOs;
using MangaVault.Application.Mappers;
using MangaVault.Application.Validators;
using MangaVault.Core.Constants;
using MangaVault.Core.Entities;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;

namespace MangaVault.Application.Services;

public class UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
{
    public User Register(CreateUserDto dto)
    {
        UserValidator.ValidateCreate(dto);

        User user = UserMapper.ToEntity(dto, passwordHasher);
        userRepository.Save(user);
        return user;
    }

    public Session Authenticate(string name, string password)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
            throw new UnauthorizedAccessException(ErrorMessages.InvalidCredentials);

        User? user = userRepository.GetByName(name);
        if (user is null || !user.VerifyPassword(password, passwordHasher))
            throw new UnauthorizedAccessException(ErrorMessages.InvalidCredentials);

        return new Session(user);
    }

    public User GetById(int userId) =>
        userRepository.GetById(userId) ?? throw new UserNotFoundException(userId);

    public IReadOnlyList<User> GetAll() => userRepository.GetAll();

    public void ChangePassword(int userId, string currentPassword, string newPassword)
    {
        UserValidator.ValidatePassword(newPassword, nameof(newPassword));

        User user = GetById(userId);
        if (!user.VerifyPassword(currentPassword, passwordHasher))
            throw new UnauthorizedAccessException(ErrorMessages.InvalidCurrentPassword);

        user.ChangePassword(newPassword, passwordHasher);
        userRepository.Update(user);
    }
}
