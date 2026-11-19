using System.Text.Json.Serialization;

namespace MangaVault.Core.Entities;

public class Collection
{
    public int Id { get; set; }

    [JsonInclude]
    public int UserId { get; private set; }

    [JsonInclude]
    public string Name { get; private set; } = "";

    [JsonInclude]
    public List<int> WorkIds
    {
        get;
        private set => field = value ?? [];
    } = [];

    public Collection() { }

    public Collection(int id, int userId, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

        Id = id;
        UserId = userId;
        Rename(name);
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        Name = newName.Trim();
    }

    public bool AddWork(int workId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workId);

        if (WorkIds.Contains(workId))
            return false;

        WorkIds.Add(workId);
        return true;
    }

    public bool RemoveWork(int workId) => WorkIds.Remove(workId);
}
