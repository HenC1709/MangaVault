using System.Text.Json.Serialization;

namespace MangaVault.Core.Entities;

public class Genre
{
    [JsonInclude]
    public string Name { get; private set; } = "";

    public string Description
    {
        get;
        set => field = (value ?? "").Trim();
    } = "";

    public Genre() { }

    public Genre(string name, string description = "")
    {
        Update(name, description);
    }

    public void Update(string name, string description = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = description;
    }
}
