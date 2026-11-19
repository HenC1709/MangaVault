namespace MangaVault.Core.ValueObjects;

public readonly record struct RatingValue
{
    public int Value { get; }

    public RatingValue(int value)
    {
        if (value is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(value), "La calificación debe estar entre 1 y 10.");

        Value = value;
    }

    public static RatingValue From(int value) => new(value);

    public override string ToString() => $"{Value}/10";
}
