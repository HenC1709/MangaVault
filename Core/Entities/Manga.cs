namespace MangaVault.Core.Entities
{
    public class Manga
    {
        public int Id { get; set; }
        public string Title { get; private set; } = "";
        public string Mangaka { get; set; } = "";
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public int VolumeCount { get; set; } = 0;
        public int FinalChapters { get; set; } = 0;
        // constructor
        public Manga()  {}

       public Manga(int id, string title, string mangaka, int volumeCount
        ,int finalChapters, List<Genre>? genres = null)
        {
            Id = id; 
            Title = title;
            Mangaka = mangaka;
            VolumeCount = volumeCount;
            FinalChapters = finalChapters;
            Genres = genres ?? new List<Genre>();
        }
    }

    public class Genre
    {
        public string Name { get; set; } ="";
        public string Description { get; set; } = "";
    }

}