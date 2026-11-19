namespace MangaVault.Core.Constants;

public static class ValidationRules
{
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 50;

    public const int MinPasswordLength = 6;
    public const int MaxPasswordLength = 128;

    public const int MaxTitleLength = 200;
    public const int MaxAuthorLength = 120;

    public const int MinRating = 1;
    public const int MaxRating = 10;
}
