namespace Segaris.Api.Persistence;

internal sealed record DatabaseCommand(DatabaseCommandKind Kind, bool Confirmed, bool Seed)
{
    public static DatabaseCommand? Parse(string[] args)
    {
        if (args.Length < 2 || !args[0].Equals("database", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var kind = args[1].ToUpperInvariant() switch
        {
            "RESET" => DatabaseCommandKind.Reset,
            "SEED" => DatabaseCommandKind.Seed,
            _ => throw new InvalidOperationException("Supported database commands are reset and seed."),
        };

        return new DatabaseCommand(
            kind,
            args.Contains("--confirm", StringComparer.OrdinalIgnoreCase),
            !args.Contains("--no-seed", StringComparer.OrdinalIgnoreCase));
    }
}

internal enum DatabaseCommandKind
{
    Reset,
    Seed,
}
