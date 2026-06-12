using System.Collections.Frozen;
using System.Globalization;

namespace Segaris.Shared.Money;

public readonly record struct CurrencyCode
{
    private static readonly FrozenSet<string> KnownCodes = CreateKnownCodes();

    public CurrencyCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var canonicalValue = value.Trim().ToUpperInvariant();
        if (canonicalValue.Length != 3 || canonicalValue.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Currency codes must contain exactly three ASCII letters.",
                nameof(value));
        }

        if (!KnownCodes.Contains(canonicalValue))
        {
            throw new ArgumentException("The currency code is not a known ISO 4217 code.", nameof(value));
        }

        Value = canonicalValue;
    }

    public string Value { get; }

    public override string ToString() => Value;

    private static FrozenSet<string> CreateKnownCodes()
    {
        var codes = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name).ISOCurrencySymbol)
            .ToHashSet(StringComparer.Ordinal);

        codes.UnionWith(
        [
            "XAG", "XAU", "XBA", "XBB", "XBC", "XBD", "XDR", "XPD", "XPT", "XSU", "XTS", "XUA",
        ]);

        return codes.ToFrozenSet(StringComparer.Ordinal);
    }
}
