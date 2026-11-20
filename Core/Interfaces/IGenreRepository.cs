using MangaVault.Core.Entities;

namespace MangaVault.Core.Interfaces;

public interface IGenreRepository
{
    List<Genre> GetAll();
    Genre? GetByName(string name);
    void Save(Genre genre);
}
