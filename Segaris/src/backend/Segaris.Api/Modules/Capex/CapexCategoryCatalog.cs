namespace Segaris.Api.Modules.Capex;

/// <summary>
/// Frozen seed definitions for the Capex-owned category catalog
/// (<c>CapexCategory</c>). Like the shared catalogs, category rows use a
/// database-assigned auto-increment <c>Id</c> with the <see cref="CapexCategorySeed.Code"/>
/// as the stable identity; display names are canonical <c>en-GB</c> values and
/// are localizable in the presentation layer.
/// </summary>
internal static class CapexCategoryCatalog
{
    public static class Codes
    {
        public const string Furniture = "FURNITURE";
        public const string Appliances = "APPLIANCES";
        public const string Technology = "TECHNOLOGY";
        public const string Home = "HOME";
        public const string FoodAndDining = "FOOD_AND_DINING";
        public const string Leisure = "LEISURE";
        public const string Health = "HEALTH";
        public const string Transport = "TRANSPORT";
        public const string Travel = "TRAVEL";
        public const string Education = "EDUCATION";
        public const string Gifts = "GIFTS";
        public const string TaxesAndFees = "TAXES_AND_FEES";
        public const string SalaryAndIncome = "SALARY_AND_INCOME";
        public const string Other = "OTHER";

        public const string Default = Other;
    }

    public static readonly IReadOnlyList<CapexCategorySeed> Categories =
    [
        new(Codes.Furniture, "Furniture"),
        new(Codes.Appliances, "Appliances"),
        new(Codes.Technology, "Technology"),
        new(Codes.Home, "Home"),
        new(Codes.FoodAndDining, "Food & Dining"),
        new(Codes.Leisure, "Leisure"),
        new(Codes.Health, "Health"),
        new(Codes.Transport, "Transport"),
        new(Codes.Travel, "Travel"),
        new(Codes.Education, "Education"),
        new(Codes.Gifts, "Gifts"),
        new(Codes.TaxesAndFees, "Taxes & Fees"),
        new(Codes.SalaryAndIncome, "Salary & Income"),
        new(Codes.Other, "Other"),
    ];
}

/// <summary>
/// A single frozen Capex category seed row: its stable <paramref name="Code"/>
/// and the canonical display <paramref name="Name"/>.
/// </summary>
internal sealed record CapexCategorySeed(string Code, string Name);
