using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
namespace MangaVault.Core.Interfaces
{
    public interface IMangaRepository
    {
        List<Manga> GetAll();
        Manga? GetById(int Id);
        void Update(Manga manga);
        void Save(Manga manga);
        void Delete (int Id);
    }
}