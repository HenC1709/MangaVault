using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Infrastructure.Persistence.Json;
using MangaVault.Infrastructure.Security;
using Xunit;

namespace MangaVault.Tests;

public sealed class UserServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"mangavault-tests-{Guid.NewGuid():N}");
    private readonly UserService _service;

    public UserServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        var repo = new JsonUserRepository(_tempDir);
        var hasher = new Pbkdf2PasswordHasher();
        _service = new UserService(repo, hasher);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Register_AssignsIdAndHashesPassword()
    {
        User user = _service.Register(new CreateUserDto("  Kirito  ", "secret123", Role.User));

        Assert.Equal(1, user.UserId);
        Assert.Equal("Kirito", user.Name);
        Assert.NotEqual("secret123", user.PasswordHash);
        Assert.StartsWith("PBKDF2-SHA256$", user.PasswordHash);
    }

    [Fact]
    public void Register_DuplicateNameCaseInsensitive_ThrowsDuplicateUserException()
    {
        _service.Register(new CreateUserDto("Asuna", "password123"));

        Assert.Throws<DuplicateUserException>(() =>
            _service.Register(new CreateUserDto("asuna", "otherpass123")));
    }

    [Theory]
    [InlineData("ab", "password123")]
    [InlineData("validUser", "12345")]
    [InlineData("", "password123")]
    public void Register_InvalidData_ThrowsArgumentException(string name, string password)
    {
        Assert.Throws<ArgumentException>(() =>
            _service.Register(new CreateUserDto(name, password)));
    }

    [Fact]
    public void Authenticate_ValidCredentials_ReturnsSession()
    {
        _service.Register(new CreateUserDto("Guts", "berserk99", Role.Admin));

        Session session = _service.Authenticate("guts", "berserk99");

        Assert.Equal("Guts", session.UserName);
        Assert.Equal(Role.Admin, session.Role);
        Assert.Equal(DateTimeKind.Utc, session.StartedAtUtc.Kind);
    }

    [Fact]
    public void Authenticate_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        _service.Register(new CreateUserDto("Casca", "falcon123"));

        Assert.Throws<UnauthorizedAccessException>(() =>
            _service.Authenticate("Casca", "wrongpass"));
    }

    [Fact]
    public void ChangePassword_ValidCurrentPassword_UpdatesHash()
    {
        User user = _service.Register(new CreateUserDto("Griffith", "oldpass123"));

        _service.ChangePassword(user.UserId, "oldpass123", "newpass456");

        Assert.Throws<UnauthorizedAccessException>(() =>
            _service.Authenticate("Griffith", "oldpass123"));

        Session session = _service.Authenticate("Griffith", "newpass456");
        Assert.Equal(user.UserId, session.UserId);
    }
}
