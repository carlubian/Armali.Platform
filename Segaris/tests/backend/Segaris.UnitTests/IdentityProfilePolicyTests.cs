using Microsoft.AspNetCore.Http;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Platform.Api;

namespace Segaris.UnitTests;

public sealed class IdentityProfilePolicyTests
{
    [Fact]
    public void Profile_values_are_trimmed_and_accept_the_supported_language()
    {
        var values = IdentityProfilePolicy.Validate("  Alice Example  ", "en-GB");

        Assert.Equal("Alice Example", values.DisplayName);
        Assert.Equal("en-GB", values.Language);
    }

    [Fact]
    public void Unsupported_languages_are_rejected()
    {
        var exception = Assert.Throws<ApiProblemException>(
            () => IdentityProfilePolicy.Validate("Alice", "es-ES"));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains("language", exception.Errors!.Keys);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void Common_avatar_image_types_are_accepted(string contentType)
    {
        IdentityProfilePolicy.ValidateAvatar(contentType);
    }

    [Fact]
    public void Non_image_avatar_types_are_rejected()
    {
        Assert.Throws<ApiProblemException>(() => IdentityProfilePolicy.ValidateAvatar("application/pdf"));
    }
}
