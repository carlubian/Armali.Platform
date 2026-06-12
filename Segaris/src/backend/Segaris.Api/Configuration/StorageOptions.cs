namespace Segaris.Api.Configuration;

internal sealed class StorageOptions
{
    public const string SectionName = "Segaris:Storage";

    public string? DataProtectionKeysPath { get; set; }

    public string? AttachmentsPath { get; set; }
}
