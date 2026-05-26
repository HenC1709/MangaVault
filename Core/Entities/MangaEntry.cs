using MangaVault.Core.Enums;

namespace MangaVault.Core.Entities
{
    public class MangaEntry
    {
        public int MangaId {get; private set;}
        public int UserId{get; private set;}
        public int CurrentChapter {get; private set;}
        public FavoriteType ReadingStatus { get; private set;}
        public DateTime DateAdded {get; private set;} 

       public MangaEntry() { }
        public MangaEntry(int userId, int mangaId, int currentChapter, FavoriteType status = FavoriteType.PlanToRead)
        {
            UserId = userId;
            MangaId = mangaId;
            CurrentChapter = currentChapter;
            ReadingStatus = status;
            DateAdded = DateTime.Now;
        }
     public void UpdateProgress(int newChapter, FavoriteType newStatus)
      {
        if (newChapter >= 0) 
        {
        CurrentChapter = newChapter;
        ReadingStatus = newStatus;
        }
      }  
    }
}