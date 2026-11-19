using System.Text.Json.Serialization;

namespace MangaVault.Core.Entities;

public class Character
{
    public int Id { get; set; }

    public int MangaId
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    }

    [JsonInclude]
    public string Name { get; private set; } = "";

    public string Role
    {
        get;
        set => field = (value ?? "").Trim();
    } = "";

    public string? Description
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public Character() { }

    public Character(int id, int mangaId, string name, string role = "", string? description = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);

        Id = id;
        MangaId = mangaId;
        SetName(name);
        Role = role;
        Description = description;
    }

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
