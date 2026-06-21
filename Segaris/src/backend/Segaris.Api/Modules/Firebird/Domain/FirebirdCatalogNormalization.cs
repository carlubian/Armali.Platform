namespace Segaris.Api.Modules.Firebird.Domain;

internal static class FirebirdCatalogNormalization
{
    public static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}

