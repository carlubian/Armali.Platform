namespace Segaris.Api.Modules.Clothes.Domain;

internal enum ClothesGarmentStatus
{
    Active,
    Unavailable,
    Deprecated,
}

internal enum WashingCare
{
    Any,
    Wash30,
    Wash30Delicate,
    Wash40,
    Wash40Delicate,
    Wash50,
    Wash50Delicate,
    Wash60,
    Wash60Delicate,
    HandWash,
    DoNotWash,
}

internal enum DryingCare
{
    Any,
    Delicate,
    VeryDelicate,
}

internal enum IroningCare
{
    Any,
    Low,
    Medium,
    DoNotIron,
}

internal enum DryCleaningCare
{
    Any,
    DoNotDryClean,
}
