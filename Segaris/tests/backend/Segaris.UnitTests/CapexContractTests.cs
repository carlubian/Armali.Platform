using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration;
using Segaris.Shared.Api;

namespace Segaris.UnitTests;

/// <summary>
/// Freezes the Wave 0 Capex and Configuration public contracts: catalog codes,
/// fixed vocabularies, query allow-lists, and stable error codes. Later Waves
/// implement behavior against exactly these values.
/// </summary>
public sealed class CapexContractTests
{
    [Fact]
    public void Configuration_supplier_codes_are_frozen_and_unique()
    {
        var codes = ConfigurationCatalog.Suppliers.Select(seed => seed.Code).ToArray();

        Assert.Equal(
            ["AMAZON", "IKEA", "CARREFOUR", "EL_CORTE_INGLES", "LEROY_MERLIN", "OTHER"],
            codes);
        Assert.All(codes, AssertStableCode);
    }

    [Fact]
    public void Configuration_cost_center_codes_are_frozen_and_unique()
    {
        var codes = ConfigurationCatalog.CostCenters.Select(seed => seed.Code).ToArray();

        Assert.Equal(["HOUSEHOLD", "PERSONAL", "WORK", "SHARED", "OTHER"], codes);
        Assert.All(codes, AssertStableCode);
    }

    [Fact]
    public void Configuration_currency_codes_are_frozen_with_euro_default()
    {
        var codes = ConfigurationCatalog.Currencies.Select(seed => seed.Code).ToArray();

        Assert.Equal(["EUR", "USD", "GBP"], codes);
        Assert.Equal("EUR", ConfigurationCatalog.CurrencyCodes.Default);
    }

    [Fact]
    public void Capex_category_codes_are_frozen_with_other_default()
    {
        var codes = CapexCategoryCatalog.Categories.Select(seed => seed.Code).ToArray();

        Assert.Equal(
            [
                "FURNITURE", "APPLIANCES", "TECHNOLOGY", "HOME", "FOOD_AND_DINING",
                "LEISURE", "HEALTH", "TRANSPORT", "TRAVEL", "EDUCATION", "GIFTS",
                "TAXES_AND_FEES", "SALARY_AND_INCOME", "OTHER",
            ],
            codes);
        Assert.Equal("OTHER", CapexCategoryCatalog.Codes.Default);
        Assert.All(codes, AssertStableCode);
    }

    [Fact]
    public void Movement_type_vocabulary_is_fixed()
    {
        Assert.Equal(["Income", "Expense"], Enum.GetNames<CapexMovementType>());
    }

    [Fact]
    public void Entry_status_vocabulary_is_fixed()
    {
        Assert.Equal(["Planning", "Completed", "Canceled"], Enum.GetNames<CapexEntryStatus>());
    }

    [Fact]
    public void Entry_sort_allow_list_is_frozen_with_id_tie_breaker()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "title", "type", "status", "dueDate", "category",
                "supplier", "costCenter", "total", "currency", "id",
            },
            CapexEntryQuery.AllowedSortFields);
        Assert.Equal("dueDate", CapexEntryQuery.SortFields.Default);
        Assert.Equal("id", CapexEntryQuery.SortFields.TieBreaker);
    }

    [Fact]
    public void Default_sort_resolves_to_due_date_descending()
    {
        var sort = SortRequest.Create(
            field: null,
            direction: "desc",
            CapexEntryQuery.AllowedSortFields,
            CapexEntryQuery.SortFields.Default,
            CapexEntryQuery.SortFields.TieBreaker);

        Assert.Equal("dueDate", sort.Field);
        Assert.Equal(SortDirection.Descending, sort.Direction);
        Assert.Equal("id", sort.TieBreakerField);
    }

    [Fact]
    public void Page_size_options_are_frozen()
    {
        Assert.Equal([10, 25, 50, 100], CapexEntryQuery.PageSizeOptions);
    }

    [Fact]
    public void Capex_error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("capex.entry.not_found", CapexErrorCodes.EntryNotFound.Value);
        Assert.Equal("capex.entry.validation", CapexErrorCodes.EntryValidation.Value);
        Assert.Equal("capex.catalog.unknown_reference", CapexErrorCodes.UnknownCatalogReference.Value);
        Assert.Equal("capex.entry.visibility_forbidden", CapexErrorCodes.VisibilityForbidden.Value);
        Assert.Equal("capex.attachment.not_found", CapexErrorCodes.AttachmentNotFound.Value);
        Assert.Equal("capex.attachment.invalid", CapexErrorCodes.AttachmentInvalid.Value);
    }

    [Fact]
    public void Attachment_owner_is_scoped_to_the_entry()
    {
        var owner = CapexAttachments.Owner(42);

        Assert.Equal("Capex", owner.Module);
        Assert.Equal("Entry", owner.EntityType);
        Assert.Equal("42", owner.EntityId);
    }

    [Fact]
    public void Launcher_card_key_is_frozen()
    {
        Assert.Equal("capex", CapexLauncherCard.ModuleKey);
    }

    private static void AssertStableCode(string code)
    {
        Assert.False(string.IsNullOrWhiteSpace(code));
        Assert.All(
            code,
            character => Assert.True(
                character is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_',
                $"Catalog codes must be upper-case ASCII identifiers: '{code}'."));
    }
}
