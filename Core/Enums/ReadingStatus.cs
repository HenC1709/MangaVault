namespace MangaVault.Core.Enums
{
    public enum ReadingStatus
    
    {
        
    Reading = 1,      // Leyendo (Los que está siguiendo activamente)
    PlanToRead = 2,   // Pendiente / Por leer (Su lista de deseos)
    Completed = 3,    // Terminado (Mangas que ya leyó completos)
    OnHold = 4,       // En pausa (Lo dejó de leer temporalmente)
    Dropped = 5       // Abandonado (No le gustó y no lo va a terminar)
    }
}