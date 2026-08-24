namespace Blackwing.Api.Configuration;

internal sealed class StorageOptions
{
    public const string SectionName = "Blackwing:Storage";

    /// <summary>Root of the image volume. Required in every environment.</summary>
    public string? ImagesPath { get; set; }

    /// <summary>Directory holding the Data Protection key ring. Optional in this phase.</summary>
    public string? DataProtectionKeysPath { get; set; }
}
