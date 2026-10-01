using Blackwing.Api.Platform.Images;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Images;

namespace Blackwing.UnitTests.Images;

/// <summary>
/// The on-disk layout is a contract with the volume that already holds people's photos, so it is
/// pinned here without touching a disk.
/// </summary>
public sealed class ImageBlobPathTests
{
    private const string Hash = "ab12cdef0123456789abcdef0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public void The_original_sits_under_the_owner_and_a_two_level_fan_out_of_two_characters_each()
    {
        var path = FileSystemImageBlobStore.GetRelativePath(new UserId(7), Hash, ImageVariant.Original);

        Assert.Equal(Path.Combine("7", "ab", "12", Hash), path);
    }

    [Fact]
    public void The_derivatives_sit_next_to_the_original_with_their_own_suffix()
    {
        var thumbnail = FileSystemImageBlobStore.GetRelativePath(new UserId(7), Hash, ImageVariant.Thumbnail);
        var preview = FileSystemImageBlobStore.GetRelativePath(new UserId(7), Hash, ImageVariant.Preview);

        Assert.Equal(Path.Combine("7", "ab", "12", $"{Hash}.thumb.webp"), thumbnail);
        Assert.Equal(Path.Combine("7", "ab", "12", $"{Hash}.preview.webp"), preview);
    }

    [Fact]
    public void The_original_keeps_no_extension()
    {
        var path = FileSystemImageBlobStore.GetRelativePath(new UserId(1), Hash, ImageVariant.Original);

        Assert.Equal(string.Empty, Path.GetExtension(path));
    }

    [Fact]
    public void Two_accounts_never_share_a_path_for_the_same_content()
    {
        var first = FileSystemImageBlobStore.GetRelativePath(new UserId(1), Hash, ImageVariant.Original);
        var second = FileSystemImageBlobStore.GetRelativePath(new UserId(2), Hash, ImageVariant.Original);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void The_path_is_made_of_lowercase_characters_only_apart_from_the_owner()
    {
        var path = FileSystemImageBlobStore.GetRelativePath(new UserId(7), Hash, ImageVariant.Preview);

        Assert.Equal(path.ToLowerInvariant(), path);
    }

    [Fact]
    public void An_uppercase_hash_is_rejected_instead_of_being_quietly_lowered()
    {
        Assert.Throws<ArgumentException>(
            () => FileSystemImageBlobStore.GetRelativePath(new UserId(7), Hash.ToUpperInvariant(), ImageVariant.Original));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab12")]
    [InlineData("ab12cdef0123456789abcdef0123456789abcdef0123456789abcdef0123456")]
    [InlineData("ab12cdef0123456789abcdef0123456789abcdef0123456789abcdef012345678")]
    public void A_hash_of_the_wrong_length_is_rejected(string hash)
    {
        Assert.Throws<ArgumentException>(
            () => FileSystemImageBlobStore.GetRelativePath(new UserId(7), hash, ImageVariant.Original));
    }

    [Theory]
    [InlineData("../../../../etc/passwd/................................................")]
    [InlineData("zz12cdef0123456789abcdef0123456789abcdef0123456789abcdef01234567")]
    public void A_hash_that_is_not_hexadecimal_is_rejected_so_it_cannot_climb_out_of_the_volume(string hash)
    {
        Assert.Throws<ArgumentException>(
            () => FileSystemImageBlobStore.GetRelativePath(new UserId(7), hash, ImageVariant.Original));
    }
}
