using Segaris.Api.Modules.Health.Domain;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class HealthDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 23, 10, 0, 0, TimeSpan.Zero);
    private static readonly UserId Creator = new(1);

    // ── Disease validation ──────────────────────────────────────────────────────

    [Fact]
    public void Disease_trims_name_and_stamps_audit()
    {
        var disease = Disease.Create(DiseaseValues(name: " Flu "), Creator, Now);

        Assert.Equal("Flu", disease.Name);
        Assert.Equal(Creator.Value, disease.CreatedBy);
        Assert.Equal(Creator.Value, disease.UpdatedBy);
        Assert.Equal(Now, disease.CreatedAt);
        Assert.Equal(Now, disease.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Disease_rejects_blank_name(string name) =>
        Assert.Throws<HealthValidationException>(() => Disease.Create(DiseaseValues(name: name), Creator, Now));

    [Fact]
    public void Disease_rejects_invalid_category() =>
        Assert.Throws<HealthValidationException>(() => Disease.Create(DiseaseValues(categoryId: 0), Creator, Now));

    [Fact]
    public void Disease_rejects_invalid_visibility() =>
        Assert.Throws<HealthValidationException>(
            () => Disease.Create(DiseaseValues(visibility: (RecordVisibility)99), Creator, Now));

    [Fact]
    public void Disease_rejects_non_utc_timestamp()
    {
        var localNow = new DateTimeOffset(2026, 6, 23, 10, 0, 0, TimeSpan.FromHours(2));
        Assert.Throws<HealthValidationException>(() => Disease.Create(DiseaseValues(), Creator, localNow));
    }

    // ── Average duration bounds (1–100,000) ─────────────────────────────────────

    [Fact]
    public void Disease_accepts_null_average_duration()
    {
        var disease = Disease.Create(DiseaseValues(averageDurationDays: null), Creator, Now);
        Assert.Null(disease.AverageDurationDays);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(100_000)]
    public void Disease_accepts_average_duration_within_bounds(int days)
    {
        var disease = Disease.Create(DiseaseValues(averageDurationDays: days), Creator, Now);
        Assert.Equal(days, disease.AverageDurationDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100_001)]
    public void Disease_rejects_average_duration_out_of_bounds(int days) =>
        Assert.Throws<HealthValidationException>(
            () => Disease.Create(DiseaseValues(averageDurationDays: days), Creator, Now));

    [Fact]
    public void Disease_replace_category_stamps_modification()
    {
        var disease = Disease.Create(DiseaseValues(categoryId: 1), Creator, Now);
        var later = Now.AddHours(1);
        disease.ReplaceCategory(2, Creator, later);

        Assert.Equal(2, disease.CategoryId);
        Assert.Equal(later, disease.UpdatedAt);
    }

    [Fact]
    public void Disease_replace_category_rejects_zero_id()
    {
        var disease = Disease.Create(DiseaseValues(), Creator, Now);
        Assert.Throws<HealthValidationException>(() => disease.ReplaceCategory(0, Creator, Now));
    }

    // ── Medicine validation and prescription flag ───────────────────────────────

    [Fact]
    public void Medicine_trims_name_and_stamps_audit()
    {
        var medicine = Medicine.Create(MedicineValues(name: " Ibuprofen "), Creator, Now);

        Assert.Equal("Ibuprofen", medicine.Name);
        Assert.Equal(Creator.Value, medicine.CreatedBy);
        Assert.Equal(Now, medicine.UpdatedAt);
    }

    [Fact]
    public void Medicine_defaults_prescription_flag_to_false()
    {
        var medicine = Medicine.Create(MedicineValues(requiresPrescription: false), Creator, Now);
        Assert.False(medicine.RequiresPrescription);
    }

    [Fact]
    public void Medicine_honours_prescription_flag_when_set()
    {
        var medicine = Medicine.Create(MedicineValues(requiresPrescription: true), Creator, Now);
        Assert.True(medicine.RequiresPrescription);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-3, null)]
    [InlineData(8, 8)]
    public void Medicine_keeps_only_positive_inventory_item_reference(int input, int? expected)
    {
        var medicine = Medicine.Create(MedicineValues(inventoryItemId: input), Creator, Now);
        Assert.Equal(expected, medicine.InventoryItemId);
    }

    [Fact]
    public void Medicine_rejects_invalid_category() =>
        Assert.Throws<HealthValidationException>(() => Medicine.Create(MedicineValues(categoryId: 0), Creator, Now));

    [Fact]
    public void Medicine_rejects_invalid_visibility() =>
        Assert.Throws<HealthValidationException>(
            () => Medicine.Create(MedicineValues(visibility: (RecordVisibility)99), Creator, Now));

    [Fact]
    public void Medicine_rejects_non_utc_timestamp()
    {
        var localNow = new DateTimeOffset(2026, 6, 23, 10, 0, 0, TimeSpan.FromHours(2));
        Assert.Throws<HealthValidationException>(() => Medicine.Create(MedicineValues(), Creator, localNow));
    }

    [Fact]
    public void Medicine_update_replaces_fields_and_clears_blank_optionals()
    {
        var medicine = Medicine.Create(MedicineValues(posology: "With food", notes: "Keep cool"), Creator, Now);
        medicine.Update(MedicineValues(posology: "", notes: null), Creator, Now.AddHours(1));

        Assert.Null(medicine.Posology);
        Assert.Null(medicine.Notes);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static DiseaseValues DiseaseValues(
        string? name = "Flu",
        int categoryId = 1,
        string? symptoms = null,
        int? averageDurationDays = null,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public) =>
        new(name, categoryId, symptoms, averageDurationDays, notes, visibility);

    private static MedicineValues MedicineValues(
        string? name = "Ibuprofen",
        int categoryId = 1,
        string? posology = null,
        bool requiresPrescription = false,
        int? inventoryItemId = null,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public) =>
        new(name, categoryId, posology, requiresPrescription, inventoryItemId, notes, visibility);
}
