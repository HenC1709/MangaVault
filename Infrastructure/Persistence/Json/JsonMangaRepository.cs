using System.Text.Json;
using MangaVault.Core.Entities;
using MangaVault.Core.Interfaces;

namespace MangaVault.Infrastructure.Persistence.Json
{
    public class JsonMangaRepository : IMangaRepository
    {
        private readonly string _path = Path.Combine("Storage", "mangas.json");

        public List<Manga> GetAll()
        {
            if (!File.Exists(_path)) return new List<Manga>();
            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<Manga>>(json) ?? new List<Manga>();
        }

         public void Delete(int id)
        {
            var mangas = GetAll();
            var ListraFiltrada = mangas.Where(m => m.Id != id).ToList();
            SaveAll(ListraFiltrada);
        }

        public Manga? GetById(int id)
        {
            return GetAll().FirstOrDefault(l => l.Id == id);
        }
        public void Update(Manga manga)
        {
            var mangas = GetAll();
            var index = mangas.FindIndex(m => m.Id == manga.Id);
            if (index != -1)
            {
                mangas[index] = manga;
                SaveAll(mangas);
            }
        }
       
        public void Save(Manga manga)
        {
            var mangas = GetAll();
            mangas.Add(manga);
            SaveAll(mangas);
        }

        private void SaveAll(List<Manga> mangas)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var opciones = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(mangas, opciones);
            File.WriteAllText(_path, json);
        }
    }
}