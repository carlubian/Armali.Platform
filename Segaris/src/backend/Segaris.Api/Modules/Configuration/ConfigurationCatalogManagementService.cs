using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Configuration;

internal sealed class ConfigurationCatalogManagementService(
    SegarisDbContext database,
    IEnumerable<ICatalogReferenceHandler> referenceHandlers,
    IClock clock)
{
    public Task<SupplierResponse> CreateSupplierAsync(CatalogItemRequest request, UserId actor, CancellationToken cancellationToken) =>
        CreateAsync<SegarisSupplier, SupplierResponse>(request, actor, static entity => new(entity.Id, entity.Name, entity.SortOrder), cancellationToken);

    public Task<CostCenterResponse> CreateCostCenterAsync(CatalogItemRequest request, UserId actor, CancellationToken cancellationToken) =>
        CreateAsync<SegarisCostCenter, CostCenterResponse>(request, actor, static entity => new(entity.Id, entity.Name, entity.SortOrder), cancellationToken);

    public async Task<CurrencyResponse> CreateCurrencyAsync(CurrencyItemRequest request, UserId actor, CancellationToken cancellationToken)
    {
        var name = ValidateName(request.Name);
        var code = ValidateCurrencyCode(request.Code);
        await EnsureUniqueCurrencyAsync(null, name.Normalized, code, cancellationToken);
        var now = clock.UtcNow;
        var entity = new SegarisCurrency
        {
            Name = name.Display,
            NormalizedName = name.Normalized,
            Code = code,
            NormalizedCode = code,
            SortOrder = await NextSortOrderAsync<SegarisCurrency>(cancellationToken),
            CreatedAt = now,
            CreatedBy = actor.Value,
            UpdatedAt = now,
            UpdatedBy = actor.Value,
        };
        database.Add(entity);
        await SaveAsync(cancellationToken);
        return new(entity.Id, entity.Code, entity.Name, entity.SortOrder);
    }

    public Task<SupplierResponse> UpdateSupplierAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken cancellationToken) =>
        UpdateAsync<SegarisSupplier, SupplierResponse>(id, request, actor, static entity => new(entity.Id, entity.Name, entity.SortOrder), cancellationToken);

    public Task<CostCenterResponse> UpdateCostCenterAsync(int id, CatalogItemRequest request, UserId actor, CancellationToken cancellationToken) =>
        UpdateAsync<SegarisCostCenter, CostCenterResponse>(id, request, actor, static entity => new(entity.Id, entity.Name, entity.SortOrder), cancellationToken);

    public async Task<CurrencyResponse> UpdateCurrencyAsync(int id, CurrencyItemRequest request, UserId actor, CancellationToken cancellationToken)
    {
        var entity = await database.Set<SegarisCurrency>().SingleOrDefaultAsync(value => value.Id == id, cancellationToken)
            ?? throw ConfigurationProblem.NotFound();
        var name = ValidateName(request.Name);
        var code = ValidateCurrencyCode(request.Code);
        await EnsureUniqueCurrencyAsync(id, name.Normalized, code, cancellationToken);
        entity.Name = name.Display;
        entity.NormalizedName = name.Normalized;
        entity.Code = code;
        entity.NormalizedCode = code;
        entity.UpdatedAt = clock.UtcNow;
        entity.UpdatedBy = actor.Value;
        await SaveAsync(cancellationToken);
        return new(entity.Id, entity.Code, entity.Name, entity.SortOrder);
    }

    public Task MoveSupplierAsync(int id, CatalogMoveDirection direction, CancellationToken cancellationToken) => MoveAsync<SegarisSupplier>(id, direction, cancellationToken);
    public Task MoveCostCenterAsync(int id, CatalogMoveDirection direction, CancellationToken cancellationToken) => MoveAsync<SegarisCostCenter>(id, direction, cancellationToken);
    public Task MoveCurrencyAsync(int id, CatalogMoveDirection direction, CancellationToken cancellationToken) => MoveAsync<SegarisCurrency>(id, direction, cancellationToken);

    public Task<CatalogDeletionImpactResponse> SupplierImpactAsync(int id, CancellationToken cancellationToken) =>
        ImpactAsync<SegarisSupplier>(id, ConfigurationCatalogKind.Suppliers, optional: true, required: false, currency: false, cancellationToken);
    public Task<CatalogDeletionImpactResponse> CostCenterImpactAsync(int id, CancellationToken cancellationToken) =>
        ImpactAsync<SegarisCostCenter>(id, ConfigurationCatalogKind.CostCenters, optional: true, required: false, currency: false, cancellationToken);
    public Task<CatalogDeletionImpactResponse> CurrencyImpactAsync(int id, CancellationToken cancellationToken) =>
        ImpactAsync<SegarisCurrency>(id, ConfigurationCatalogKind.Currencies, optional: false, required: true, currency: true, cancellationToken);

    public Task DeleteSupplierAsync(int id, CancellationToken cancellationToken) => DeleteAsync<SegarisSupplier>(id, ConfigurationCatalogKind.Suppliers, required: false, cancellationToken);
    public Task DeleteCostCenterAsync(int id, CancellationToken cancellationToken) => DeleteAsync<SegarisCostCenter>(id, ConfigurationCatalogKind.CostCenters, required: false, cancellationToken);
    public Task DeleteCurrencyAsync(int id, CancellationToken cancellationToken) => DeleteAsync<SegarisCurrency>(id, ConfigurationCatalogKind.Currencies, required: true, cancellationToken);

    public Task ReplaceAndDeleteSupplierAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken cancellationToken) =>
        ReplaceAndDeleteAsync<SegarisSupplier>(id, ConfigurationCatalogKind.Suppliers, request, actor, cancellationToken);

    public Task ReplaceAndDeleteCostCenterAsync(int id, CatalogReplacementRequest request, UserId actor, CancellationToken cancellationToken) =>
        ReplaceAndDeleteAsync<SegarisCostCenter>(id, ConfigurationCatalogKind.CostCenters, request, actor, cancellationToken);

    private async Task<TResponse> CreateAsync<TEntity, TResponse>(CatalogItemRequest request, UserId actor, Func<TEntity, TResponse> response, CancellationToken cancellationToken)
        where TEntity : class, IConfigurationCatalogEntity, new()
    {
        var name = ValidateName(request.Name);
        await EnsureUniqueNameAsync<TEntity>(null, name.Normalized, cancellationToken);
        var now = clock.UtcNow;
        var entity = new TEntity
        {
            Name = name.Display,
            NormalizedName = name.Normalized,
            SortOrder = await NextSortOrderAsync<TEntity>(cancellationToken),
            CreatedAt = now,
            CreatedBy = actor.Value,
            UpdatedAt = now,
            UpdatedBy = actor.Value,
        };
        database.Add(entity);
        await SaveAsync(cancellationToken);
        return response(entity);
    }

    private async Task<TResponse> UpdateAsync<TEntity, TResponse>(int id, CatalogItemRequest request, UserId actor, Func<TEntity, TResponse> response, CancellationToken cancellationToken)
        where TEntity : class, IConfigurationCatalogEntity
    {
        var entity = await database.Set<TEntity>().SingleOrDefaultAsync(value => value.Id == id, cancellationToken)
            ?? throw ConfigurationProblem.NotFound();
        var name = ValidateName(request.Name);
        await EnsureUniqueNameAsync<TEntity>(id, name.Normalized, cancellationToken);
        entity.Name = name.Display;
        entity.NormalizedName = name.Normalized;
        entity.UpdatedAt = clock.UtcNow;
        entity.UpdatedBy = actor.Value;
        await SaveAsync(cancellationToken);
        return response(entity);
    }

    private async Task MoveAsync<TEntity>(int id, CatalogMoveDirection direction, CancellationToken cancellationToken)
        where TEntity : class, IConfigurationCatalogEntity
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var ordered = await database.Set<TEntity>().OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(cancellationToken);
        var index = ordered.FindIndex(value => value.Id == id);
        if (index < 0) throw ConfigurationProblem.NotFound();
        var target = direction == CatalogMoveDirection.Up ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count) throw ConfigurationProblem.Validation("direction", "The value cannot move beyond the catalog boundary.");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++) ordered[position].SortOrder = position;
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<CatalogDeletionImpactResponse> ImpactAsync<TEntity>(int id, ConfigurationCatalogKind kind, bool optional, bool required, bool currency, CancellationToken cancellationToken)
        where TEntity : class, IConfigurationCatalogEntity
    {
        var exists = await database.Set<TEntity>().AsNoTracking().AnyAsync(value => value.Id == id, cancellationToken);
        if (!exists) throw ConfigurationProblem.NotFound();
        var referenced = await HasReferencesAsync(kind, id, cancellationToken);
        var count = await database.Set<TEntity>().CountAsync(cancellationToken);
        return new(referenced, !referenced && (!required || count > 1), referenced && optional, referenced && currency, count > 1);
    }

    private async Task DeleteAsync<TEntity>(int id, ConfigurationCatalogKind kind, bool required, CancellationToken cancellationToken)
        where TEntity : class, IConfigurationCatalogEntity
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var entity = await database.Set<TEntity>().SingleOrDefaultAsync(value => value.Id == id, cancellationToken)
            ?? throw ConfigurationProblem.NotFound();
        if (required && await database.Set<TEntity>().CountAsync(cancellationToken) <= 1) throw ConfigurationProblem.RequiredNotEmpty();
        if (await HasReferencesAsync(kind, id, cancellationToken)) throw ConfigurationProblem.Referenced();
        database.Remove(entity);
        var remaining = await database.Set<TEntity>().Where(value => value.Id != id).OrderBy(value => value.SortOrder).ThenBy(value => value.Id).ToListAsync(cancellationToken);
        for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw ConfigurationProblem.Referenced();
        }
    }

    private async Task ReplaceAndDeleteAsync<TEntity>(
        int id,
        ConfigurationCatalogKind kind,
        CatalogReplacementRequest request,
        UserId actor,
        CancellationToken cancellationToken)
        where TEntity : class, IConfigurationCatalogEntity
    {
        ValidateOptionalReplacement(id, request);

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var source = await database.Set<TEntity>().SingleOrDefaultAsync(value => value.Id == id, cancellationToken)
            ?? throw ConfigurationProblem.NotFound();

        if (request.ReplacementId is { } replacementId
            && !await database.Set<TEntity>().AnyAsync(value => value.Id == replacementId, cancellationToken))
        {
            throw ConfigurationProblem.InvalidReplacement();
        }

        var migration = new CatalogReferenceMigration(
            kind,
            id,
            request.ReplacementId,
            request.ClearReferences,
            ExchangeRate: null,
            actor,
            clock.UtcNow);

        try
        {
            foreach (var handler in referenceHandlers.Where(value => value.Kind == kind))
            {
                await handler.MigrateReferencesAsync(migration, cancellationToken);
            }

            database.Remove(source);
            var remaining = await database.Set<TEntity>()
                .Where(value => value.Id != id)
                .OrderBy(value => value.SortOrder)
                .ThenBy(value => value.Id)
                .ToListAsync(cancellationToken);
            for (var position = 0; position < remaining.Count; position++) remaining[position].SortOrder = position;

            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw ConfigurationProblem.MigrationConflict();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw ConfigurationProblem.MigrationFailed();
        }
    }

    private static void ValidateOptionalReplacement(int sourceId, CatalogReplacementRequest request)
    {
        var hasReplacement = request.ReplacementId is not null;
        if (request.ExchangeRate is not null
            || hasReplacement == request.ClearReferences
            || request.ReplacementId == sourceId
            || request.ReplacementId is <= 0)
        {
            throw ConfigurationProblem.InvalidReplacement();
        }
    }

    private async Task<bool> HasReferencesAsync(ConfigurationCatalogKind kind, int id, CancellationToken cancellationToken)
    {
        foreach (var handler in referenceHandlers.Where(value => value.Kind == kind))
            if (await handler.HasReferencesAsync(id, cancellationToken)) return true;
        return false;
    }

    private async Task<int> NextSortOrderAsync<TEntity>(CancellationToken cancellationToken) where TEntity : class, IConfigurationCatalogEntity =>
        (await database.Set<TEntity>().Select(value => (int?)value.SortOrder).MaxAsync(cancellationToken) ?? -1) + 1;

    private async Task EnsureUniqueNameAsync<TEntity>(int? id, string normalizedName, CancellationToken cancellationToken) where TEntity : class, IConfigurationCatalogEntity
    {
        if (await database.Set<TEntity>().AnyAsync(value => value.NormalizedName == normalizedName && value.Id != id, cancellationToken))
            throw ConfigurationProblem.DuplicateName();
    }

    private async Task EnsureUniqueCurrencyAsync(int? id, string normalizedName, string normalizedCode, CancellationToken cancellationToken)
    {
        if (await database.Set<SegarisCurrency>().AnyAsync(value => value.NormalizedName == normalizedName && value.Id != id, cancellationToken)) throw ConfigurationProblem.DuplicateName();
        if (await database.Set<SegarisCurrency>().AnyAsync(value => value.NormalizedCode == normalizedCode && value.Id != id, cancellationToken)) throw ConfigurationProblem.DuplicateCode();
    }

    private static (string Display, string Normalized) ValidateName(string? value)
    {
        var display = value?.Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > CatalogNormalization.NameMaximumLength)
            throw ConfigurationProblem.Validation("name", $"Name is required and may contain at most {CatalogNormalization.NameMaximumLength} characters.");
        return (display, CatalogNormalization.Normalize(display));
    }

    private static string ValidateCurrencyCode(string? value)
    {
        var code = value?.Trim().ToUpperInvariant();
        if (code is null || code.Length != 3 || code.Any(character => character is < 'A' or > 'Z')) throw ConfigurationProblem.InvalidCode();
        return code;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try { await database.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw ConfigurationProblem.DuplicateName(); }
    }
}
