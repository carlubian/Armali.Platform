using Blackwing.Api.Platform.Api;
using Blackwing.Api.Platform.Images;
using Microsoft.AspNetCore.Http;

namespace Blackwing.UnitTests.Images;

/// <summary>
/// The upload policy trusts bytes, not names. These tests pin the three accepted formats and each
/// way a file can be turned away.
/// </summary>
public sealed class ImagePolicyTests
{
    private static readonly byte[] Jpeg = [0xff, 0xd8, 0xff, 0xe0, 0x00, 0x10, 0x4a, 0x46, 0x49, 0x46, 0x00, 0x01];
    private static readonly byte[] Png = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00, 0x00, 0x00, 0x0d];
    private static readonly byte[] Webp = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBP"u8, .. "VP8 "u8];

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.JPG", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    public void Every_accepted_extension_resolves_to_its_content_type(string fileName, string expected)
    {
        var normalized = ImagePolicy.NormalizeAndValidateFileName(fileName);

        Assert.Equal(fileName, normalized);
        Assert.Equal(expected, ImagePolicy.ResolveContentType(normalized));
    }

    [Fact]
    public void Surrounding_whitespace_is_trimmed_from_the_file_name()
    {
        Assert.Equal("photo.jpg", ImagePolicy.NormalizeAndValidateFileName("  photo.jpg  "));
    }

    [Theory]
    [InlineData("photo.gif")]
    [InlineData("photo.heic")]
    [InlineData("photo.pdf")]
    [InlineData("photo")]
    public void An_extension_that_is_not_allowed_is_rejected_as_an_unsupported_format(string fileName)
    {
        var problem = Assert.Throws<ApiProblemException>(
            () => ImagePolicy.NormalizeAndValidateFileName(fileName));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(ImageErrorCodes.UnsupportedFormat, problem.Code);
    }

    [Theory]
    [InlineData("pho\u0000to.jpg")]
    [InlineData("pho\nto.jpg")]
    [InlineData("pho\u0007to.jpg")]
    public void A_file_name_with_control_characters_is_rejected(string fileName)
    {
        var problem = Assert.Throws<ApiProblemException>(
            () => ImagePolicy.NormalizeAndValidateFileName(fileName));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Contains("fileName", problem.Errors!.Keys);
    }

    [Theory]
    [InlineData("folder/photo.jpg")]
    [InlineData("folder\\photo.jpg")]
    [InlineData("../photo.jpg")]
    public void A_file_name_that_carries_a_path_is_rejected(string fileName)
    {
        var problem = Assert.Throws<ApiProblemException>(
            () => ImagePolicy.NormalizeAndValidateFileName(fileName));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_file_name_is_rejected(string? fileName)
    {
        var problem = Assert.Throws<ApiProblemException>(
            () => ImagePolicy.NormalizeAndValidateFileName(fileName));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public void A_file_name_of_255_characters_is_accepted_and_one_more_is_not()
    {
        var accepted = new string('a', ImagePolicy.MaximumFileNameLength - ".jpg".Length) + ".jpg";
        var rejected = new string('a', ImagePolicy.MaximumFileNameLength - ".jpg".Length + 1) + ".jpg";

        Assert.Equal(accepted, ImagePolicy.NormalizeAndValidateFileName(accepted));
        Assert.Throws<ApiProblemException>(() => ImagePolicy.NormalizeAndValidateFileName(rejected));
    }

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpg", "image/jpeg; charset=binary")]
    [InlineData("photo.png", "IMAGE/PNG")]
    [InlineData("photo.webp", null)]
    [InlineData("photo.webp", "")]
    public void A_declared_content_type_that_matches_the_extension_or_is_absent_is_accepted(
        string fileName,
        string? declared)
    {
        ImagePolicy.EnsureDeclaredContentTypeMatches(fileName, declared);
    }

    [Theory]
    [InlineData("photo.jpg", "image/png")]
    [InlineData("photo.png", "application/octet-stream")]
    [InlineData("photo.webp", "image/jpeg")]
    public void A_declared_content_type_that_contradicts_the_extension_is_rejected(
        string fileName,
        string declared)
    {
        var problem = Assert.Throws<ApiProblemException>(
            () => ImagePolicy.EnsureDeclaredContentTypeMatches(fileName, declared));

        Assert.Equal(ImageErrorCodes.ContentMismatch, problem.Code);
    }

    [Theory]
    [MemberData(nameof(ValidContent))]
    public void Content_that_matches_its_extension_validates_and_rewinds_the_stream(string fileName, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        stream.Position = 5;

        ImagePolicy.ValidateContent(fileName, stream);

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void A_jpg_whose_bytes_are_a_png_is_rejected_and_the_stream_is_rewound()
    {
        using var stream = new MemoryStream(Png);

        var problem = Assert.Throws<ApiProblemException>(
            () => ImagePolicy.ValidateContent("photo.jpg", stream));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(ImageErrorCodes.ContentMismatch, problem.Code);
        Assert.Equal(0, stream.Position);
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.png")]
    [InlineData("photo.webp")]
    public void An_empty_file_is_rejected_whatever_its_extension(string fileName)
    {
        using var stream = new MemoryStream();

        var problem = Assert.Throws<ApiProblemException>(() => ImagePolicy.ValidateContent(fileName, stream));

        Assert.Equal(ImageErrorCodes.ContentMismatch, problem.Code);
    }

    [Fact]
    public void A_riff_container_that_is_not_webp_is_rejected()
    {
        byte[] wave = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WAVE"u8, .. "fmt "u8];
        using var stream = new MemoryStream(wave);

        Assert.Throws<ApiProblemException>(() => ImagePolicy.ValidateContent("photo.webp", stream));
    }

    [Fact]
    public void A_truncated_webp_header_is_rejected()
    {
        using var stream = new MemoryStream([.. "RIFF"u8, 0x24, 0x00]);

        Assert.Throws<ApiProblemException>(() => ImagePolicy.ValidateContent("photo.webp", stream));
    }

    [Fact]
    public void Detection_names_each_accepted_format_and_nothing_else()
    {
        Assert.Equal("image/jpeg", ImagePolicy.DetectContentType(Jpeg));
        Assert.Equal("image/png", ImagePolicy.DetectContentType(Png));
        Assert.Equal("image/webp", ImagePolicy.DetectContentType(Webp));
        Assert.Null(ImagePolicy.DetectContentType("GIF89a"u8));
        Assert.Null(ImagePolicy.DetectContentType([]));
    }

    public static TheoryData<string, byte[]> ValidContent() => new()
    {
        { "photo.jpg", Jpeg },
        { "photo.jpeg", Jpeg },
        { "photo.png", Png },
        { "photo.webp", Webp },
    };
}
