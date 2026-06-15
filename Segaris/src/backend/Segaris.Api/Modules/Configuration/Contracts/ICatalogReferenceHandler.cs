using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Configuration.Contracts;

/// <summary>
/// Narrow reference-migration contract published by Configuration and implemented
/// by the modules that reference a shared catalog (initially Capex). The owning
/// Configuration deletion command resolves every handler for a catalog
/// <see cref="Kind"/> and drives them inside the single <c>SegarisDbContext</c>
/// transaction it started.
///
/// Transaction ownership is fixed: a handler mutates tracked entities and updates
/// their audit metadata, but it must never call <c>SaveChanges</c> or commit. The
/// owner performs one final save and commit only after every handler succeeds; if
/// any handler or validation step fails, the whole transaction rolls back and no
/// reference, amount, audit field, or catalog row changes.
///
/// Keeping the interface in the Configuration namespace preserves the
/// <c>Capex -> Configuration</c> dependency direction: Capex implements and
/// registers handlers, Configuration never references Capex.
/// </summary>
internal interface ICatalogReferenceHandler
{
    /// <summary>The shared catalog whose references this handler migrates.</summary>
    ConfigurationCatalogKind Kind { get; }

    /// <summary>
    /// Reports whether any record references the catalog value without revealing
    /// counts, identities, or other private details. Includes public and private
    /// records.
    /// </summary>
    Task<bool> HasReferencesAsync(int catalogId, CancellationToken cancellationToken);

    /// <summary>
    /// Migrates every reference to <paramref name="migration"/>'s source according
    /// to the requested mode (replace, clear, or currency conversion) and updates
    /// the affected records' modification metadata. Does not save or commit.
    /// </summary>
    Task MigrateReferencesAsync(
        CatalogReferenceMigration migration,
        CancellationToken cancellationToken);
}

/// <summary>
/// The fully validated migration a handler must apply to its references. Exactly
/// one mode is active: replacement (<see cref="ReplacementId"/> set), clearing
/// (<see cref="ClearReferences"/> true), or currency conversion
/// (<see cref="ExchangeRate"/> set alongside a replacement). The owner validates
/// the source, replacement, and per-catalog rules before constructing this value.
/// </summary>
/// <param name="Kind">The catalog being migrated.</param>
/// <param name="SourceId">The catalog value being removed.</param>
/// <param name="ReplacementId">The target value, when references are replaced.</param>
/// <param name="ClearReferences">References are cleared to <c>null</c>.</param>
/// <param name="ExchangeRate">
/// The positive conversion rate (<c>1 source = ExchangeRate target</c>), set only
/// for a referenced currency conversion.
/// </param>
/// <param name="Actor">The administrator performing the migration, for audit.</param>
/// <param name="OccurredAt">The UTC modification time stamped on affected records.</param>
internal sealed record CatalogReferenceMigration(
    ConfigurationCatalogKind Kind,
    int SourceId,
    int? ReplacementId,
    bool ClearReferences,
    decimal? ExchangeRate,
    UserId Actor,
    DateTimeOffset OccurredAt);
