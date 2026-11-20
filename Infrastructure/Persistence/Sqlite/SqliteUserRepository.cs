using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace MangaVault.Infrastructure.Persistence.Sqlite;

public sealed class SqliteUserRepository(SqliteDatabase database) : IUserRepository
{
    private readonly SqliteDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    public List<User> GetAll()
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT UserId, Name, PasswordHash, Role
            FROM Users
            ORDER BY UserId;
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        var users = new List<User>();
        while (reader.Read())
        {
            users.Add(MapUser(reader));
        }

        return users;
    }

    public User? GetById(int id)
    {
        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT UserId, Name, PasswordHash, Role
            FROM Users
            WHERE UserId = @userId;
            """;
        command.Parameters.AddWithValue("@userId", id);

        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? MapUser(reader) : null;
    }

    public User? GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT UserId, Name, PasswordHash, Role
            FROM Users
            WHERE Name = @name COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("@name", name.Trim());

        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? MapUser(reader) : null;
    }

    public void Save(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();

        bool assignGeneratedId = user.UserId <= 0;
        command.CommandText = assignGeneratedId
            ? """
              INSERT INTO Users (Name, PasswordHash, Role)
              VALUES (@name, @passwordHash, @role);
              SELECT last_insert_rowid();
              """
            : """
              INSERT INTO Users (UserId, Name, PasswordHash, Role)
              VALUES (@userId, @name, @passwordHash, @role);
              """;

        if (!assignGeneratedId)
        {
            command.Parameters.AddWithValue("@userId", user.UserId);
        }

        command.Parameters.AddWithValue("@name", user.Name);
        command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@role", (int)user.Role);

        try
        {
            if (assignGeneratedId)
            {
                object? scalar = command.ExecuteScalar();
                int generatedId = Convert.ToInt32(scalar);
                user.AssignId(generatedId);
            }
            else
            {
                command.ExecuteNonQuery();
            }
        }
        catch (SqliteException ex) when (SqliteErrorCodes.IsPrimaryKeyViolation(ex))
        {
            throw new DuplicateUserException(user.UserId);
        }
        catch (SqliteException ex) when (SqliteErrorCodes.IsUniqueViolation(ex))
        {
            throw new DuplicateUserException(user.Name);
        }
    }

    public void Update(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        using SqliteConnection connection = _database.OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET Name = @name,
                PasswordHash = @passwordHash,
                Role = @role
            WHERE UserId = @userId;
            """;
        command.Parameters.AddWithValue("@userId", user.UserId);
        command.Parameters.AddWithValue("@name", user.Name);
        command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@role", (int)user.Role);

        int affected;
        try
        {
            affected = command.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (SqliteErrorCodes.IsUniqueViolation(ex))
        {
            throw new DuplicateUserException(user.Name);
        }

        if (affected == 0)
            throw new UserNotFoundException(user.UserId);
    }

    private static User MapUser(SqliteDataReader reader) =>
        new(
            userId: reader.GetInt32(0),
            name: reader.GetString(1),
            passwordHash: reader.GetString(2),
            rol: (Role)reader.GetInt32(3));
}
