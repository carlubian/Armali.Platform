namespace Segaris.Shared.Api;

public sealed record SortRequest(
    string Field,
    SortDirection Direction,
    string TieBreakerField)
{
    public static SortRequest Create(
        string? field,
        string? direction,
        IReadOnlySet<string> allowedFields,
        string defaultField,
        string tieBreakerField)
    {
        ArgumentNullException.ThrowIfNull(allowedFields);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultField);
        ArgumentException.ThrowIfNullOrWhiteSpace(tieBreakerField);

        var selectedField = string.IsNullOrWhiteSpace(field) ? defaultField : field;
        if (!allowedFields.Contains(selectedField))
        {
            throw new ArgumentException("The requested sort field is not allowed.", nameof(field));
        }

        if (!allowedFields.Contains(tieBreakerField))
        {
            throw new ArgumentException("The stable tie-breaker field must be allowed.", nameof(tieBreakerField));
        }

        var selectedDirection = direction?.ToLowerInvariant() switch
        {
            null or "" or "asc" => SortDirection.Ascending,
            "desc" => SortDirection.Descending,
            _ => throw new ArgumentException("Sort direction must be 'asc' or 'desc'.", nameof(direction)),
        };

        return new SortRequest(selectedField, selectedDirection, tieBreakerField);
    }
}
