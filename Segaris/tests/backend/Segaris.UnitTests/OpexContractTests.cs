using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Opex;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class OpexContractTests
{
    [Fact]
    public void Fixed_vocabularies_are_frozen()
    {
        Assert.Equal(["Income", "Expense"], Enum.GetNames<OpexMovementType>());
        Assert.Equal(["Planning", "Active", "OnHold", "Closed"], Enum.GetNames<OpexContractStatus>());
        Assert.Equal(
            ["None", "Weekly", "Monthly", "Quarterly", "SemiAnnual", "Annual", "Irregular"],
            Enum.GetNames<OpexExpectedFrequency>());
    }

    [Fact]
    public void Creation_defaults_are_frozen()
    {
        Assert.Equal(OpexMovementType.Expense, OpexDefaults.MovementType);
        Assert.Equal(OpexContractStatus.Planning, OpexDefaults.Status);
        Assert.Equal(OpexExpectedFrequency.None, OpexDefaults.ExpectedFrequency);
        Assert.Equal(RecordVisibility.Public, OpexDefaults.Visibility);
        Assert.Equal(0.00m, OpexDefaults.OccurrenceAmount);
        Assert.Equal("Europe/Madrid", OpexDefaults.HouseholdTimeZoneId);
        Assert.Equal(
            new DateOnly(2026, 1, 1),
            OpexDefaults.OccurrenceDate(
                new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Routes_are_nested_under_contracts()
    {
        Assert.Equal("opex/contracts", OpexApiRoutes.Contracts);
        Assert.Equal("/{contractId:int}", OpexApiRoutes.ContractById);
        Assert.Equal("/{contractId:int}/occurrences", OpexApiRoutes.Occurrences);
        Assert.Equal(
            "/{contractId:int}/occurrences/{occurrenceId:int}",
            OpexApiRoutes.OccurrenceById);
        Assert.Equal("opex/categories", OpexApiRoutes.Categories);
    }

    [Fact]
    public void Contract_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "name", "type", "status", "category", "supplier", "frequency",
                "estimatedAnnualAmount", "realizedCurrentYearAmount", "currency", "id",
            },
            OpexContractQuery.AllowedSortFields);
        Assert.Equal("name", OpexContractQuery.SortFields.Default);
        Assert.Equal("id", OpexContractQuery.SortFields.TieBreaker);
        Assert.Equal([10, 25, 50, 100], OpexContractQuery.PageSizeOptions);
        Assert.Equal([10, 25, 50, 100], OpexOccurrenceQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_contract_sort_is_name_ascending()
    {
        var sort = SortRequest.Create(
            null,
            null,
            OpexContractQuery.AllowedSortFields,
            OpexContractQuery.SortFields.Default,
            OpexContractQuery.SortFields.TieBreaker);

        Assert.Equal("name", sort.Field);
        Assert.Equal(SortDirection.Ascending, sort.Direction);
        Assert.Equal("id", sort.TieBreakerField);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Pagination_rejects_page_sizes_outside_platform_bounds(int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PaginationRequest(1, pageSize));
    }

    [Fact]
    public void Shared_configuration_references_are_explicit()
    {
        Assert.Equal(
            [ConfigurationCatalogKind.Suppliers, ConfigurationCatalogKind.CostCenters, ConfigurationCatalogKind.Currencies],
            OpexConfigurationContracts.SharedReferenceKinds);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("opex.contract.not_found", OpexErrorCodes.ContractNotFound.Value);
        Assert.Equal("opex.contract.validation", OpexErrorCodes.ContractValidation.Value);
        Assert.Equal("opex.contract.duplicate_name", OpexErrorCodes.ContractDuplicateName.Value);
        Assert.Equal("opex.occurrence.not_found", OpexErrorCodes.OccurrenceNotFound.Value);
        Assert.Equal("opex.occurrence.validation", OpexErrorCodes.OccurrenceValidation.Value);
        Assert.Equal("opex.catalog.unknown_reference", OpexErrorCodes.UnknownCatalogReference.Value);
    }

    [Fact]
    public void Attachment_owners_distinguish_contracts_and_occurrences()
    {
        var contract = OpexAttachments.ContractOwner(12);
        var occurrence = OpexAttachments.OccurrenceOwner(34);

        Assert.Equal(("Opex", "Contract", "12"), (contract.Module, contract.EntityType, contract.EntityId));
        Assert.Equal(("Opex", "Occurrence", "34"), (occurrence.Module, occurrence.EntityType, occurrence.EntityId));
    }

    [Fact]
    public void Request_and_response_shapes_keep_occurrences_subordinate()
    {
        var request = new CreateOpexOccurrenceRequest(
            new DateOnly(2026, 6, 15), 12.34m, "Invoice", null);
        var response = new OpexOccurrenceResponse(
            8, 3, request.EffectiveDate!.Value, request.ActualAmount,
            request.Description, request.Notes, [], 1, "Owner",
            DateTimeOffset.UnixEpoch, null, null, DateTimeOffset.UnixEpoch);

        Assert.Equal(3, response.ContractId);
        Assert.Equal(12.34m, response.ActualAmount);
        Assert.DoesNotContain(
            typeof(OpexOccurrenceResponse).GetProperties(),
            property => property.Name is "CurrencyId" or "MovementType" or "Visibility");
    }
}
