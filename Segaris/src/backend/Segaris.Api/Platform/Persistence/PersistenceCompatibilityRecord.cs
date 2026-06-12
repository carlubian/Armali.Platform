namespace Segaris.Api.Platform.Persistence;

public sealed class PersistenceCompatibilityRecord
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateOnly CivilDate { get; set; }

    public decimal Amount { get; set; }

    public required string CurrencyCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
