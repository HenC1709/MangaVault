using MangaVault.Core.Entities;

namespace MangaVault.Core.Interfaces;

public interface IFavoriteRepository
{
    List<MangaEntry> GetAll();
    List<MangaEntry> GetByUser(int userId);
    MangaEntry? Get(int userId, int mangaId);
    void Save(MangaEntry entry);
    void Update(MangaEntry entry);
    void Delete(int userId, int mangaId);
}
