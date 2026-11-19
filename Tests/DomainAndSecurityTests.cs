using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.ValueObjects;
using MangaVault.Infrastructure.Security;
using Xunit;

namespace MangaVault.Tests;

public sealed class DomainAndSecurityTests
{
    [Fact]
    public void Pbkdf2PasswordHasher_GeneratesUniqueSaltsAndVerifies()
    {
        var hasher = new Pbkdf2PasswordHasher();

        string hash1 = hasher.Hash("MangaVault2026!");
        string hash2 = hasher.Hash("MangaVault2026!");

        Assert.NotEqual(hash1, hash2);
        Assert.True(hasher.Verify("MangaVault2026!", hash1));
        Assert.True(hasher.Verify("MangaVault2026!", hash2));
        Assert.False(hasher.Verify("WrongPassword", hash1));
    }

    [Fact]
    public void Email_NormalizesValidAddressAndRejectsInvalid()
    {
        var email = new Email("  Reader.Manga@Example.COM  ");
        Assert.Equal("reader.manga@example.com", email.Value);

        Assert.Throws<ArgumentException>(() => new Email("invalid-email"));
        Assert.Throws<ArgumentException>(() => new Email("user@localhost"));
    }

    [Fact]
    public void RatingValue_ValidatesBounds()
    {
        var rating = RatingValue.From(9);
        Assert.Equal(9, rating.Value);
        Assert.Equal("9/10", rating.ToString());

        Assert.Throws<ArgumentOutOfRangeException>(() => new RatingValue(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RatingValue(11));
    }

    [Fact]
    public void Collection_ManagesUniqueWorks()
    {
        var collection = new Collection(1, 1, "Pendientes 2026");

        Assert.True(collection.AddWork(5));
        Assert.False(collection.AddWork(5));
        Assert.Single(collection.WorkIds);

        Assert.True(collection.RemoveWork(5));
        Assert.Empty(collection.WorkIds);
    }

    [Fact]
    public void MangaEntry_UsesUtcTimestamp()
    {
        var entry = new MangaEntry(1, 1, 0, ReadingStatus.PlanToRead);
        Assert.Equal(DateTimeKind.Utc, entry.DateAdded.Kind);
    }
}
