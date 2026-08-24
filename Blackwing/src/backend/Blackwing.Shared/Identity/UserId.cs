using System.Globalization;

namespace Blackwing.Shared.Identity;

/// <summary>
/// A validated account identifier. Zero and negative values are rejected on
/// construction, which is what lets the ownership filter treat zero as "no user"
/// without ever colliding with a real account. See <c>BlackwingDbContext</c>.
/// </summary>
public readonly record struct UserId
{
    public UserId(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
