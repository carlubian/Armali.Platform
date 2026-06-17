using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Travel;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class TravelContractTests
{
    [Fact]
    public void Fixed_vocabularies_are_frozen()
    {
        Assert.Equal(["Planned", "Ongoing", "Completed", "Cancelled"], Enum.GetNames<TravelTripStatus>());
    }

    [Fact]
    public void Creation_defaults_are_frozen()
    {
        Assert.Equal(TravelTripStatus.Planned, TravelDefaults.TripStatus);
        Assert.Equal(RecordVisibility.Public, TravelDefaults.Visibility);
        Assert.Equal(0.00m, TravelDefaults.ExpenseAmount);
        Assert.Equal("Europe/Madrid", TravelDefaults.HouseholdTimeZoneId);

        var newYearEve = new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 1, 1), TravelDefaults.Today(newYearEve));
    }

    [Fact]
    public void Routes_freeze_trips_expenses_catalogs_and_attachments()
    {
        Assert.Equal("travel/trips", TravelApiRoutes.Trips);
        Assert.Equal("/{tripId:int}", TravelApiRoutes.TripById);
        Assert.Equal("/{tripId:int}/attachments", TravelApiRoutes.TripAttachments);
        Assert.Equal("/{tripId:int}/attachments/{attachmentId}", TravelApiRoutes.TripAttachmentById);
        Assert.Equal("/{tripId:int}/expenses", TravelApiRoutes.TripExpenses);
        Assert.Equal("/{tripId:int}/expenses/{expenseId:int}", TravelApiRoutes.TripExpenseById);
        Assert.Equal("/{tripId:int}/expenses/{expenseId:int}/attachments", TravelApiRoutes.TripExpenseAttachments);
        Assert.Equal(
            "/{tripId:int}/expenses/{expenseId:int}/attachments/{attachmentId}",
            TravelApiRoutes.TripExpenseAttachmentById);
        Assert.Equal("travel/trip-types", TravelApiRoutes.TripTypes);
        Assert.Equal("travel/expense-categories", TravelApiRoutes.ExpenseCategories);
    }

    [Fact]
    public void Trip_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "name", "tripType", "destination", "startDate", "endDate", "status", "visibility", "id",
            },
            TravelTripQuery.AllowedSortFields);
        Assert.Equal("startDate", TravelTripQuery.SortFields.Default);
        Assert.Equal("id", TravelTripQuery.SortFields.TieBreaker);
        Assert.Equal("desc", TravelTripQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], TravelTripQuery.PageSizeOptions);
    }

    [Fact]
    public void Expense_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "date", "category", "description", "amount", "currency", "supplier", "costCenter", "id",
            },
            TravelExpenseQuery.AllowedSortFields);
        Assert.Equal("date", TravelExpenseQuery.SortFields.Default);
        Assert.Equal("id", TravelExpenseQuery.SortFields.TieBreaker);
        Assert.Equal("asc", TravelExpenseQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], TravelExpenseQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_trip_sort_is_start_date_descending()
    {
        var sort = SortRequest.Create(
            null,
            TravelTripQuery.DefaultSortDirection,
            TravelTripQuery.AllowedSortFields,
            TravelTripQuery.SortFields.Default,
            TravelTripQuery.SortFields.TieBreaker);

        Assert.Equal("startDate", sort.Field);
        Assert.Equal(SortDirection.Descending, sort.Direction);
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
            [
                ConfigurationCatalogKind.Suppliers,
                ConfigurationCatalogKind.Currencies,
                ConfigurationCatalogKind.CostCenters,
            ],
            TravelConfigurationContracts.SharedReferenceKinds);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("travel.trip.not_found", TravelErrorCodes.TripNotFound.Value);
        Assert.Equal("travel.trip.validation", TravelErrorCodes.TripValidation.Value);
        Assert.Equal("travel.itinerary.validation", TravelErrorCodes.ItineraryValidation.Value);
        Assert.Equal("travel.expense.not_found", TravelErrorCodes.ExpenseNotFound.Value);
        Assert.Equal("travel.expense.validation", TravelErrorCodes.ExpenseValidation.Value);
        Assert.Equal("travel.catalog.unknown_reference", TravelErrorCodes.UnknownCatalogReference.Value);
        Assert.Equal("travel.attachment.invalid", TravelErrorCodes.AttachmentInvalid.Value);
        Assert.Equal("travel.trip_type.not_found", TravelErrorCodes.TripTypeNotFound.Value);
        Assert.Equal(
            "travel.expense_category.not_found",
            TravelErrorCodes.ExpenseCategoryNotFound.Value);
    }

    [Fact]
    public void Attachment_owners_distinguish_trips_and_expenses()
    {
        var trip = TravelAttachments.TripOwner(12);
        var expense = TravelAttachments.ExpenseOwner(34);

        Assert.Equal(("Travel", "Trip", "12"), (trip.Module, trip.EntityType, trip.EntityId));
        Assert.Equal(("Travel", "Expense", "34"), (expense.Module, expense.EntityType, expense.EntityId));
    }

    [Fact]
    public void Itinerary_and_expense_requests_do_not_carry_visibility()
    {
        Assert.DoesNotContain(
            typeof(TravelItineraryEntryRequest).GetProperties(),
            property => property.Name is "Visibility");
        Assert.DoesNotContain(
            typeof(CreateTravelExpenseRequest).GetProperties(),
            property => property.Name is "Visibility");
    }
}
