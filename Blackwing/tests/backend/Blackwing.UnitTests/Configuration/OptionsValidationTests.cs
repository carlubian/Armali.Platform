using Blackwing.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace Blackwing.UnitTests.Configuration;

/// <summary>
/// The options validators are the startup gate: a bad configuration must fail the host instead of
/// surfacing on the first request. These tests pin both directions of every rule.
/// </summary>
public sealed class OptionsValidationTests
{
    private const string ValidConnectionString =
        "Host=localhost;Port=5432;Database=blackwing;Username=blackwing;Password=blackwing";

    [Fact]
    public void Database_validator_rejects_a_missing_connection_string()
    {
        var validator = new DatabaseOptionsValidator(BuildConfiguration(connectionString: string.Empty));

        var result = validator.Validate(name: null, new DatabaseOptions());

        Assert.True(result.Failed);
        Assert.Contains("ConnectionStrings:Blackwing", result.FailureMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public void Database_validator_rejects_a_provider_other_than_postgres()
    {
        var validator = new DatabaseOptionsValidator(BuildConfiguration(ValidConnectionString));

        var result = validator.Validate(name: null, new DatabaseOptions { Provider = "Sqlite" });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Database_validator_accepts_a_valid_configuration()
    {
        var validator = new DatabaseOptionsValidator(BuildConfiguration(ValidConnectionString));

        var result = validator.Validate(name: null, new DatabaseOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Storage_validator_rejects_a_missing_images_path()
    {
        var validator = new StorageOptionsValidator();

        var result = validator.Validate(name: null, new StorageOptions { ImagesPath = string.Empty });

        Assert.True(result.Failed);
        Assert.Contains("ImagesPath", result.FailureMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public void Storage_validator_accepts_an_images_path_without_a_keys_path()
    {
        var validator = new StorageOptionsValidator();

        var result = validator.Validate(
            name: null,
            new StorageOptions { ImagesPath = "images.local", DataProtectionKeysPath = string.Empty });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Image_validator_accepts_the_defaults()
    {
        var validator = new ImageOptionsValidator();

        var result = validator.Validate(name: null, new ImageOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1024L * 1024 - 1)]
    [InlineData(1024L * 1024 * 1024 + 1)]
    [InlineData(-1L)]
    public void Image_validator_rejects_a_maximum_file_size_outside_1_mb_and_1_gb(long bytes)
    {
        var validator = new ImageOptionsValidator();

        var result = validator.Validate(name: null, new ImageOptions { MaximumFileSizeBytes = bytes });

        Assert.True(result.Failed);
        Assert.Contains("MaximumFileSizeBytes", result.FailureMessage!, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1024L * 1024)]
    [InlineData(1024L * 1024 * 1024)]
    public void Image_validator_accepts_the_edges_of_the_maximum_file_size_range(long bytes)
    {
        var validator = new ImageOptionsValidator();

        var result = validator.Validate(name: null, new ImageOptions { MaximumFileSizeBytes = bytes });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(8193)]
    public void Image_validator_rejects_a_thumbnail_edge_outside_16_and_8192_pixels(int edge)
    {
        var validator = new ImageOptionsValidator();

        var result = validator.Validate(name: null, new ImageOptions { ThumbnailLongestEdge = edge });

        Assert.True(result.Failed);
        Assert.Contains("ThumbnailLongestEdge", result.FailureMessage!, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(8193)]
    public void Image_validator_rejects_a_preview_edge_outside_16_and_8192_pixels(int edge)
    {
        var validator = new ImageOptionsValidator();

        var result = validator.Validate(name: null, new ImageOptions { PreviewLongestEdge = edge });

        Assert.True(result.Failed);
        Assert.Contains("PreviewLongestEdge", result.FailureMessage!, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(400, 400)]
    [InlineData(800, 400)]
    public void Image_validator_requires_the_preview_to_be_strictly_larger_than_the_thumbnail(
        int thumbnail,
        int preview)
    {
        var validator = new ImageOptionsValidator();

        var result = validator.Validate(
            name: null,
            new ImageOptions { ThumbnailLongestEdge = thumbnail, PreviewLongestEdge = preview });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Image_validator_accepts_a_preview_one_pixel_larger_than_the_thumbnail()
    {
        var validator = new ImageOptionsValidator();

        var result = validator.Validate(
            name: null,
            new ImageOptions { ThumbnailLongestEdge = 400, PreviewLongestEdge = 401 });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Image_validator_rejects_a_thumbnail_quality_outside_1_and_100(int quality)
    {
        var validator = new ImageOptionsValidator();

        var result = validator.Validate(name: null, new ImageOptions { ThumbnailQuality = quality });

        Assert.True(result.Failed);
        Assert.Contains("ThumbnailQuality", result.FailureMessage!, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Image_validator_rejects_a_preview_quality_outside_1_and_100(int quality)
    {
        var validator = new ImageOptionsValidator();

        var result = validator.Validate(name: null, new ImageOptions { PreviewQuality = quality });

        Assert.True(result.Failed);
        Assert.Contains("PreviewQuality", result.FailureMessage!, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Image_validator_accepts_the_edges_of_the_quality_range(int quality)
    {
        var validator = new ImageOptionsValidator();

        var result = validator.Validate(
            name: null,
            new ImageOptions { ThumbnailQuality = quality, PreviewQuality = quality });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Observability_validator_rejects_an_unknown_minimum_level()
    {
        var validator = new ObservabilityOptionsValidator();

        var result = validator.Validate(
            name: null,
            new ObservabilityOptions { Seq = new SeqOptions { MinimumLevel = "Chatty" } });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Observability_validator_rejects_enabled_seq_without_a_server_url()
    {
        var validator = new ObservabilityOptionsValidator();

        var result = validator.Validate(
            name: null,
            new ObservabilityOptions { Seq = new SeqOptions { Enabled = true, ServerUrl = string.Empty } });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Observability_validator_accepts_disabled_seq()
    {
        var validator = new ObservabilityOptionsValidator();

        var result = validator.Validate(name: null, new ObservabilityOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Observability_validator_accepts_enabled_seq_with_an_absolute_url()
    {
        var validator = new ObservabilityOptionsValidator();

        var result = validator.Validate(
            name: null,
            new ObservabilityOptions
            {
                Seq = new SeqOptions { Enabled = true, ServerUrl = "http://localhost:5341" },
            });

        Assert.True(result.Succeeded);
    }

    private static IConfiguration BuildConfiguration(string connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:Blackwing"] = connectionString,
            })
            .Build();
}
