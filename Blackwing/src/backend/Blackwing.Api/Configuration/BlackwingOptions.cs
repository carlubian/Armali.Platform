using System.ComponentModel.DataAnnotations;

namespace Blackwing.Api.Configuration;

public sealed class BlackwingOptions
{
    public const string SectionName = "Blackwing";
    [Required] public StorageOptions Storage { get; init; } = new();
}
public sealed class StorageOptions { [Required] public string ImagesPath { get; init; } = string.Empty; }
