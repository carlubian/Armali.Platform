using Blackwing.Shared.Content;

namespace Blackwing.UnitTests.Content;

/// <summary>
/// The normalizer is the key tags are unique by, so these tests pin exactly which spellings
/// collide and which stay apart.
/// </summary>
public sealed class TagNormalizerTests
{
    [Theory]
    [InlineData("antonio", "ANTONIO")]
    [InlineData("Antonio", "ANTONIO")]
    [InlineData("  Antonio  ", "ANTONIO")]
    [InlineData("\tAntonio\n", "ANTONIO")]
    public void Case_and_surrounding_whitespace_do_not_matter(string typed, string expected)
    {
        Assert.Equal(expected, TagNormalizer.Normalize(typed));
    }

    [Theory]
    [InlineData("Peñíscola", "PENISCOLA")]
    [InlineData("peniscola", "PENISCOLA")]
    [InlineData("Ávila", "AVILA")]
    [InlineData("Zürich", "ZURICH")]
    public void Diacritics_are_dropped(string typed, string expected)
    {
        Assert.Equal(expected, TagNormalizer.Normalize(typed));
    }

    [Fact]
    public void An_accented_and_an_unaccented_spelling_collapse_to_the_same_key()
    {
        Assert.Equal(TagNormalizer.Normalize("Peñíscola"), TagNormalizer.Normalize("peniscola"));
    }

    [Theory]
    [InlineData("Semana   Santa", "SEMANA SANTA")]
    [InlineData("Semana\t\tSanta", "SEMANA SANTA")]
    [InlineData("  Semana \n Santa  ", "SEMANA SANTA")]
    public void Runs_of_inner_whitespace_collapse_to_a_single_space(string typed, string expected)
    {
        Assert.Equal(expected, TagNormalizer.Normalize(typed));
    }

    [Fact]
    public void A_decomposed_and_a_precomposed_accent_give_the_same_key()
    {
        const string Precomposed = "é"; // é as one code point
        const string Decomposed = "é"; // e followed by a combining acute accent

        Assert.Equal(TagNormalizer.Normalize(Precomposed), TagNormalizer.Normalize(Decomposed));
    }

    [Fact]
    public void Different_words_stay_different()
    {
        Assert.NotEqual(TagNormalizer.Normalize("Elena"), TagNormalizer.Normalize("Elenas"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_value_that_is_empty_once_trimmed_is_rejected(string typed)
    {
        Assert.Throws<ArgumentException>(() => TagNormalizer.Normalize(typed));
    }

    [Fact]
    public void A_value_made_only_of_combining_marks_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => TagNormalizer.Normalize("́"));
    }

    [Fact]
    public void A_null_value_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => TagNormalizer.Normalize(null!));
    }
}
