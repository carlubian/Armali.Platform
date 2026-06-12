using Segaris.Shared.Api;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Money;
using Segaris.Shared.Records;

namespace Segaris.UnitTests;

public sealed class SharedPrimitiveTests
{
    [Fact]
    public void User_id_requires_a_positive_value()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UserId(0));
        Assert.Equal("42", new UserId(42).ToString());
    }

    [Theory]
    [InlineData("eur", "EUR")]
    [InlineData(" USD ", "USD")]
    public void Currency_code_is_canonicalized(string input, string expected)
    {
        Assert.Equal(expected, new CurrencyCode(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData("ZZZ")]
    public void Currency_code_rejects_invalid_values(string input)
    {
        Assert.Throws<ArgumentException>(() => new CurrencyCode(input));
    }

    [Fact]
    public void Metadata_requires_utc_instants()
    {
        var localInstant = new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.FromHours(2));

        Assert.Throws<ArgumentException>(() => new CreationMetadata(localInstant, null));
        Assert.Throws<ArgumentException>(() => new ModificationMetadata(localInstant, null));
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Pagination_rejects_values_outside_the_shared_bounds(int page, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PaginationRequest(page, pageSize));
    }

    [Fact]
    public void Pagination_calculates_a_checked_offset()
    {
        var pagination = new PaginationRequest(3, 25);

        Assert.Equal(50, pagination.Offset);
    }

    [Fact]
    public void Sorting_is_allow_listed_and_includes_a_stable_tie_breaker()
    {
        var allowedFields = new HashSet<string>(StringComparer.Ordinal) { "id", "name" };

        var sort = SortRequest.Create("name", "desc", allowedFields, "name", "id");

        Assert.Equal("name", sort.Field);
        Assert.Equal(SortDirection.Descending, sort.Direction);
        Assert.Equal("id", sort.TieBreakerField);
        Assert.Throws<ArgumentException>(() =>
            SortRequest.Create("createdAt", "asc", allowedFields, "name", "id"));
    }

    [Theory]
    [InlineData("resource.not_found")]
    [InlineData("request.invalid_value")]
    public void Error_code_accepts_stable_machine_readable_values(string value)
    {
        Assert.Equal(value, new ErrorCode(value).Value);
    }

    [Theory]
    [InlineData("Resource.NotFound")]
    [InlineData("resource..missing")]
    [InlineData("resource-missing")]
    public void Error_code_rejects_unstable_formats(string value)
    {
        Assert.Throws<ArgumentException>(() => new ErrorCode(value));
    }

    [Fact]
    public void Private_visibility_is_creator_only_even_for_administrators()
    {
        var creator = new UserId(1);
        var administrator = new StubCurrentUser(new UserId(2), PlatformRole.Admin);

        Assert.False(VisibilityPolicy.CanAccess(RecordVisibility.Private, creator, administrator));
        Assert.True(VisibilityPolicy.CanAccess(
            RecordVisibility.Private,
            creator,
            new StubCurrentUser(creator, PlatformRole.User)));
    }

    [Fact]
    public void Public_visibility_still_requires_an_authenticated_user()
    {
        Assert.False(VisibilityPolicy.CanAccess(
            RecordVisibility.Public,
            new UserId(1),
            new StubCurrentUser(null)));
    }

    private sealed class StubCurrentUser(UserId? userId, params PlatformRole[] roles) : ICurrentUser
    {
        public bool IsAuthenticated => userId is not null;

        public UserId? UserId => userId;

        public bool IsInRole(PlatformRole role) => roles.Contains(role);
    }
}
