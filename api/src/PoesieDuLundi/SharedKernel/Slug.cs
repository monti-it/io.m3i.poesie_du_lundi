using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PoesieDuLundi.SharedKernel;

/// <summary>
/// A URL-safe identifier (<c>/poems/{slug}</c>) — lowercase ASCII words separated by single
/// hyphens. Self-validating: a value object never carries a shape its constructor didn't accept.
/// </summary>
public sealed partial class Slug : ValueObject
{
    public string Value { get; }

    public Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Slug cannot be empty.", nameof(value));
        }

        if (!ValidFormat().IsMatch(value))
        {
            throw new ArgumentException(
                $"'{value}' is not a valid slug — expected lowercase letters, digits, and single hyphens.",
                nameof(value));
        }

        Value = value;
    }

    /// <summary>Derives a slug from free text (a poem title) — strips accents/punctuation, lowercases,
    /// and collapses whitespace/repeated separators into single hyphens.</summary>
    public static Slug FromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Cannot derive a slug from empty text.", nameof(text));
        }

        var normalized = text.Normalize(NormalizationForm.FormD);
        var withoutDiacritics = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                withoutDiacritics.Append(c);
            }
        }

        var lowered = withoutDiacritics.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var hyphenated = NonAlphanumeric().Replace(lowered, "-");
        var collapsed = RepeatedHyphens().Replace(hyphenated, "-").Trim('-');

        if (collapsed.Length == 0)
        {
            throw new ArgumentException($"'{text}' has no slug-able characters.", nameof(text));
        }

        return new Slug(collapsed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex ValidFormat();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();

    [GeneratedRegex("-{2,}")]
    private static partial Regex RepeatedHyphens();
}
