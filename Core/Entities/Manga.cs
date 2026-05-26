using MangaVault.Core.Enums;

namespace MangaVault.Core.Entities
{
    public class Manga
    {
        public int Id { get; set; }
        public string Title { get; private set; } = "";
        public string Mangaka { get; set; } = "";
        public int VolumeCount { get; set; } = 0;
        public int FinalChapters { get; set; } = 0;
        public MangaStatus Status { get; set; } = MangaStatus.Ongoing;
        // constructor
        public Manga()  {}

       public Manga(int id, string title, string mangaka, int volumeCount
        ,int finalChapters)
        {
            Id = id; 
            Title = title;
            Mangaka = mangaka;
            VolumeCount = volumeCount;
            FinalChapters = finalChapters;
        }
    }

    
}