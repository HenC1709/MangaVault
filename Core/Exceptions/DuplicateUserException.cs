namespace MangaVault.Core.Exceptions
{
    public class DuplicateUserException : Exception
    {
        public string Name {get;}

        public DuplicateUserException(string name) :
        base($"Ya existe un usuario con el nombre '{name}'")
        {
            Name = name;
        }
    }
}