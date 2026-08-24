using Blackwing.Api.Modules.Identity;
using Microsoft.AspNetCore.Identity;

namespace Blackwing.UnitTests.Identity;

/// <summary>
/// A duplicate account is a state conflict, not a malformed request. Reporting it as 400 would
/// have a client retry the same body forever, so the mapping is pinned in both directions.
/// </summary>
public sealed class IdentityProblemTests
{
    [Theory]
    [InlineData("DuplicateUserName")]
    [InlineData("DuplicateEmail")]
    [InlineData("DuplicateRoleName")]
    public void A_duplicate_error_becomes_409(string code)
    {
        var result = IdentityResult.Failed(new IdentityError
        {
            Code = code,
            Description = "Already taken.",
        });

        var problem = IdentityProblem.FromResult(result, "userName");

        Assert.Equal(409, problem.StatusCode);
        Assert.Equal("resource.conflict", problem.Code.Value);
        Assert.Equal("Already taken.", problem.Detail);

        // A conflict carries no field errors: there is nothing about the request to correct.
        Assert.Null(problem.Errors);
    }

    [Theory]
    [InlineData("PasswordTooShort")]
    [InlineData("InvalidUserName")]
    public void Any_other_error_becomes_400_keyed_by_the_offending_field(string code)
    {
        var result = IdentityResult.Failed(new IdentityError
        {
            Code = code,
            Description = "Passwords must be at least 12 characters.",
        });

        var problem = IdentityProblem.FromResult(result, "password");

        Assert.Equal(400, problem.StatusCode);
        Assert.Equal("request.invalid", problem.Code.Value);
        Assert.NotNull(problem.Errors);
        Assert.Equal(
            ["Passwords must be at least 12 characters."],
            problem.Errors!["password"]);
    }

    [Fact]
    public void A_duplicate_among_several_errors_still_wins_the_conflict_status()
    {
        var result = IdentityResult.Failed(
            new IdentityError { Code = "PasswordTooShort", Description = "Too short." },
            new IdentityError { Code = "DuplicateUserName", Description = "Already taken." });

        var problem = IdentityProblem.FromResult(result, "userName");

        Assert.Equal(409, problem.StatusCode);
        Assert.Contains("Already taken.", problem.Detail!, StringComparison.Ordinal);
    }
}
