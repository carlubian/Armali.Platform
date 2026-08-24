namespace Blackwing.Shared.Api;

/// <summary>
/// A stable, machine-readable error identifier carried by every problem response.
/// Clients branch on this value, never on the human-readable title or detail, so the
/// wording of a message can change without breaking a caller.
/// </summary>
public readonly record struct ErrorCode
{
    public ErrorCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!IsValid(value))
        {
            throw new ArgumentException(
                "Error codes must use lowercase dot-separated ASCII identifiers.",
                nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;

    private static bool IsValid(string value)
    {
        if (value[0] == '.' || value[^1] == '.')
        {
            return false;
        }

        var previousWasDot = false;
        foreach (var character in value)
        {
            if (character == '.')
            {
                if (previousWasDot)
                {
                    return false;
                }

                previousWasDot = true;
                continue;
            }

            previousWasDot = false;
            if (character is not (>= 'a' and <= 'z') and not (>= '0' and <= '9') and not '_')
            {
                return false;
            }
        }

        return true;
    }
}
