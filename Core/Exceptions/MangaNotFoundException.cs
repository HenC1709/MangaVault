namespace MangaVault.Core.Exceptions
{
    public class MangaNotFoundException : Exception
    {
        public int MangaId { get; }

        public MangaNotFoundException(int mangaId)
            : base($"No se encontró una obra con el Id {mangaId}.")
        {
            MangaId = mangaId;
        }
    }
}
