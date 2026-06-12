using System.Globalization;

namespace Segaris.Shared.Identity;

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
