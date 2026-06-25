using System.Text.Json;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Calendar;
using Segaris.Api.Modules.Calendar.Contracts;
using Segaris.Api.Modules.Calendar.Projection;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Travel.Contracts;

namespace Segaris.UnitTests;

public sealed class CalendarContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Routes_and_query_parameters_are_frozen()
    {
        Assert.Equal("Calendar", CalendarApiRoutes.Tag);
        Assert.Equal("calendar", CalendarApiRoutes.Calendar);
        Assert.Equal("/entries", CalendarApiRoutes.Entries);
        Assert.Equal("/notes", CalendarApiRoutes.Notes);
        Assert.Equal("/notes/{noteId:int}", CalendarApiRoutes.NoteById);
        Assert.Equal("from", CalendarApiRoutes.QueryParameters.From);
        Assert.Equal("to", CalendarApiRoutes.QueryParameters.To);
        Assert.Equal("sourceModule", CalendarApiRoutes.QueryParameters.SourceModule);
        Assert.Equal("visualFamily", CalendarApiRoutes.QueryParameters.VisualFamily);
    }

    [Fact]
    public void Source_modules_source_types_and_visual_families_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "firebird", "travel", "inventory", "assets", "maintenance", "processes" },
            CalendarSourceModules.InitialProjectionSources);
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "calendar", "firebird", "travel", "inventory", "assets", "maintenance", "processes" },
            CalendarSourceModules.AllowedFilters);

        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "dailyNote",
                "birthday",
                "trip",
                "inventoryOrderExpectedReceipt",
                "assetExpectedEndOfLife",
                "maintenanceTaskDue",
                "processStepDue",
            },
            CalendarSourceTypes.InitialSourceTypes);
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "Birthday", "Travel", "Note", "Other" },
            CalendarVisualFamilies.AllowedFilters);
    }

    [Fact]
    public void Calendar_contributes_no_launcher_attention_contract()
    {
        Assert.False(CalendarModuleContract.ContributesLauncherAttention);
    }

    [Fact]
    public void Date_range_bounds_are_frozen()
    {
        Assert.Equal(366, CalendarEntriesQuery.MaximumRangeDays);

        var from = new DateOnly(2026, 1, 1);
        var accepted = CalendarEntriesQuery.Create(from, from.AddDays(365));

        Assert.Equal((from, from.AddDays(365)), (accepted.From, accepted.To));
        Assert.Throws<ArgumentException>(() => CalendarEntriesQuery.Create(from.AddDays(1), from));
        Assert.Throws<ArgumentOutOfRangeException>(() => CalendarEntriesQuery.Create(from, from.AddDays(366)));
    }

    [Fact]
    public void Filter_parsing_accepts_only_allow_listed_values()
    {
        var filter = CalendarEntriesQuery.Create(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            ["travel", "calendar", "travel"],
            ["Travel", "Note"]);

        Assert.Equal(["travel", "calendar"], filter.SourceModules.ToArray());
        Assert.Equal(["Travel", "Note"], filter.VisualFamilies.ToArray());
        Assert.Throws<ArgumentException>(() => CalendarEntriesQuery.Create(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            ["capex"],
            null));
        Assert.Throws<ArgumentException>(() => CalendarEntriesQuery.Create(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            null,
            ["Finance"]));
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("calendar.entries.range_invalid", CalendarErrorCodes.EntryRangeInvalid.Value);
        Assert.Equal("calendar.entries.source_module_unsupported", CalendarErrorCodes.EntrySourceModuleUnsupported.Value);
        Assert.Equal("calendar.entries.visual_family_unsupported", CalendarErrorCodes.EntryVisualFamilyUnsupported.Value);
        Assert.Equal("calendar.note.not_found", CalendarErrorCodes.NoteNotFound.Value);
        Assert.Equal("calendar.note.validation", CalendarErrorCodes.NoteValidation.Value);
        Assert.Equal("calendar.note.visibility_forbidden", CalendarErrorCodes.NoteVisibilityForbidden.Value);
    }

    [Fact]
    public void Entry_response_serializes_to_the_frozen_wire_shape()
    {
        var entry = new CalendarEntryResponse(
            "travel:42",
            "travel",
            "trip",
            "Travel",
            "Paris",
            "France",
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 5),
            true,
            "Active",
            "/travel?tripId=42");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(entry, Web));

        Assert.Equal("travel:42", document.RootElement.GetProperty("id").GetString());
        Assert.Equal("sourceModule", document.RootElement.EnumerateObject().ElementAt(1).Name);
        Assert.Equal("2026-06-01", document.RootElement.GetProperty("startDate").GetString());
        Assert.True(document.RootElement.GetProperty("isAllDay").GetBoolean());
    }

    [Fact]
    public void Source_owned_projection_contracts_are_explicit()
    {
        Assert.Equal(
            [
                typeof(IFirebirdCalendarProjectionProvider),
                typeof(ITravelCalendarProjectionProvider),
                typeof(IInventoryCalendarProjectionProvider),
                typeof(IAssetsCalendarProjectionProvider),
                typeof(IMaintenanceCalendarProjectionProvider),
                typeof(IProcessesCalendarProjectionProvider),
            ],
            CalendarProjectionContracts.InitialProviderContracts);
    }
}
