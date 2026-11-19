using MangaVault.Core.Entities;

namespace MangaVault.Core.Interfaces;

public interface IMangaRepository
{
    List<Manga> GetAll();
    Manga? GetById(int id);
    void Update(Manga manga);
    void Save(Manga manga);
    void Delete(int id);
}
