using Blackwing.Persistence.Gallery;

namespace Blackwing.Api.Tests;

public sealed class GalleryDomainTests
{
    [Theory]
    [InlineData("Beach", "BEACH")]
    [InlineData("  beach  ", "BEACH")]
    [InlineData("New   York", "NEW YORK")]
    [InlineData("café", "CAFÉ")]
    public void Tag_normalization_is_case_and_whitespace_insensitive(string value, string expected) =>
        Assert.Equal(expected, TagNormalization.Normalize(value));

    [Fact]
    public void Tag_labels_that_differ_only_by_case_or_spacing_share_a_normalized_key()
    {
        var owner = Guid.NewGuid();
        var first = Tag.Create(owner, TagType.Place, "Grandma's House");
        var second = Tag.Create(owner, TagType.Place, "  grandma's   house ");
        Assert.Equal(first.NormalizedValue, second.NormalizedValue);
        Assert.Equal("Grandma's House", first.Value); // Display value preserves the original casing/spacing.
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Tag_requires_a_non_empty_value(string value) =>
        Assert.Throws<ArgumentException>(() => Tag.Create(Guid.NewGuid(), TagType.Person, value));

    [Fact]
    public void Tag_rejects_a_value_beyond_the_maximum_length() =>
        Assert.Throws<ArgumentException>(() => Tag.Create(Guid.NewGuid(), TagType.Topic, new string('x', Tag.ValueMaxLength + 1)));

    [Fact]
    public void Image_starts_pending_review_and_lowercases_its_hash()
    {
        var image = Image.Create(
            new ImageValues("ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789", "image/jpeg", 4000, 3000, 1_234_567, null),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
        Assert.Null(image.ReviewedAt);
        Assert.Equal("abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789", image.Sha256);
    }

    [Fact]
    public void Image_marking_reviewed_is_idempotent()
    {
        var image = Image.Create(
            new ImageValues(new string('a', 64), "image/png", 10, 10, 100, null),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
        var first = new DateTimeOffset(2026, 7, 10, 13, 0, 0, TimeSpan.Zero);
        image.MarkReviewed(first);
        image.MarkReviewed(new DateTimeOffset(2026, 7, 10, 14, 0, 0, TimeSpan.Zero));
        Assert.Equal(first, image.ReviewedAt);
    }

    [Theory]
    [InlineData("not-a-hash")]
    [InlineData("abc")]
    public void Image_rejects_an_invalid_sha256(string sha) =>
        Assert.Throws<ArgumentException>(() => Image.Create(
            new ImageValues(sha, "image/jpeg", 10, 10, 100, null),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Image_rejects_a_non_utc_capture_date() =>
        Assert.Throws<ArgumentException>(() => Image.Create(
            new ImageValues(new string('b', 64), "image/jpeg", 10, 10, 100, new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.FromHours(2))),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero)));
}
