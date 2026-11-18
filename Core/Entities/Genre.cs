using System.Text.Json.Serialization;

namespace MangaVault.Core.Entities
{
    public class Genre
    {
        private string _description = "";

        [JsonInclude]
        public string Name { get; private set; } = "";

        public string Description
        {
            get => _description;
            set => _description = (value ?? "").Trim();
        }

        public Genre() { }

        public Genre(string name, string description = "")
        {
            Update(name, description);
        }

        public void Update(string name, string description = "")
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("El nombre del género no puede estar vacío.", nameof(name));
            }

            Name = name.Trim();
            Description = description;
        }
    }
}