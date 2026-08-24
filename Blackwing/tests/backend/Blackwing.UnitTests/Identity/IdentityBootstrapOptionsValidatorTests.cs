using Blackwing.Api.Modules.Identity.Configuration;

namespace Blackwing.UnitTests.Identity;

/// <summary>
/// The bootstrap section is the only way a fresh deployment gets its first administrator. Half a
/// section almost always means a misspelt environment variable, and the validator turns that into
/// a failure to start rather than a deployment nobody can log into.
/// </summary>
public sealed class IdentityBootstrapOptionsValidatorTests
{
    [Fact]
    public void An_empty_section_is_valid_and_means_no_bootstrap()
    {
        var validator = new IdentityBootstrapOptionsValidator();

        var result = validator.Validate(name: null, new IdentityBootstrapOptions());

        Assert.True(result.Succeeded);
        Assert.False(new IdentityBootstrapOptions().IsConfigured);
    }

    [Fact]
    public void A_user_name_without_a_password_is_rejected()
    {
        var validator = new IdentityBootstrapOptionsValidator();

        var result = validator.Validate(
            name: null,
            new IdentityBootstrapOptions { UserName = "founder" });

        Assert.True(result.Failed);
        Assert.Contains("Password", result.FailureMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public void A_password_without_a_user_name_is_rejected()
    {
        var validator = new IdentityBootstrapOptionsValidator();

        var result = validator.Validate(
            name: null,
            new IdentityBootstrapOptions { Password = "FounderPass123!" });

        Assert.True(result.Failed);
        Assert.Contains("UserName", result.FailureMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public void Both_values_together_are_accepted()
    {
        var validator = new IdentityBootstrapOptionsValidator();
        var options = new IdentityBootstrapOptions
        {
            UserName = "founder",
            Password = "FounderPass123!",
        };

        var result = validator.Validate(name: null, options);

        Assert.True(result.Succeeded);
        Assert.True(options.IsConfigured);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    public void Whitespace_counts_as_absent(string password)
    {
        var validator = new IdentityBootstrapOptionsValidator();

        var result = validator.Validate(
            name: null,
            new IdentityBootstrapOptions { UserName = "founder", Password = password });

        Assert.True(result.Failed);
    }
}
