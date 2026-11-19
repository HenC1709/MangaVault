using System.Net.Mail;

namespace MangaVault.Core.ValueObjects;

public readonly record struct Email
{
    public string Value { get; }

    public Email(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        string normalized = value.Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(normalized, out MailAddress? addr)
            || !string.Equals(addr.Address, normalized, StringComparison.Ordinal)
            || !addr.Host.Contains('.')
            || addr.Host.StartsWith('.')
            || addr.Host.EndsWith('.'))
        {
            throw new ArgumentException("Formato de correo electrónico no válido.", nameof(value));
        }

        Value = normalized;
    }

    public static Email Parse(string value) => new(value);

    public override string ToString() => Value;
}
