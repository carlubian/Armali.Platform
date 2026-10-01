using System.Net;
using Blackwing.Api.IntegrationTests.Identity;
using Blackwing.Api.IntegrationTests.Infrastructure;
using Blackwing.Shared.Images;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.IntegrationTests.Images;

/// <summary>
/// The privacy perimeter, demonstrated over the thing it exists to protect: another account's
/// photos. Everything else in the milestone rests on this being true, so it is asserted from
/// outside the process, with the exact identifier of a real image. Acceptance criterion 6.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ImageIsolationTests(PostgresFixture postgres)
{
    private static readonly string[] ReadRoutes = ["", "/thumb", "/preview", "/original"];

    /// <summary>Case 15.</summary>
    [Fact]
    public async Task Another_account_gets_404_for_the_exact_identifier_on_the_metadata_and_every_variant()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice", "bob");
        var aliceImage = await ImageTestEnvironment.UploadOkAsync(environment["alice"].Client, "private.jpg", TestImages.Jpeg());

        foreach (var route in ReadRoutes)
        {
            using var response = await environment["bob"].Client.GetAsync(
                $"/api/images/{aliceImage}{route}",
                CancellationToken.None);

            // 404, never 403: a 403 would confirm that the identifier exists.
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task The_owner_still_reaches_every_route_for_the_same_identifier()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice", "bob");
        var aliceImage = await ImageTestEnvironment.UploadOkAsync(environment["alice"].Client, "private.jpg", TestImages.Jpeg());

        foreach (var route in ReadRoutes)
        {
            using var response = await environment["alice"].Client.GetAsync(
                $"/api/images/{aliceImage}{route}",
                CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    /// <summary>Case 16. Administration is a surface over accounts, not over content.</summary>
    [Fact]
    public async Task An_administrator_also_gets_404_on_the_metadata_and_every_variant()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var aliceImage = await ImageTestEnvironment.UploadOkAsync(environment["alice"].Client, "private.jpg", TestImages.Jpeg());

        foreach (var route in ReadRoutes)
        {
            using var response = await environment.Admin.GetAsync($"/api/images/{aliceImage}{route}", CancellationToken.None);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        using var delete = await IdentityTestServer.SendWithCsrfAsync(
            environment.Admin,
            HttpMethod.Delete,
            $"/api/images/{aliceImage}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    /// <summary>Case 17.</summary>
    [Fact]
    public async Task Another_account_cannot_delete_an_image_and_the_owner_keeps_it_with_its_files()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice", "bob");
        var alice = environment["alice"];
        var jpeg = TestImages.Jpeg();
        var aliceImage = await ImageTestEnvironment.UploadOkAsync(alice.Client, "private.jpg", jpeg);
        var hash = ImageTestEnvironment.Sha256(jpeg);

        using var attempt = await IdentityTestServer.SendWithCsrfAsync(
            environment["bob"].Client,
            HttpMethod.Delete,
            $"/api/images/{aliceImage}");

        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);
        using var stillThere = await alice.Client.GetAsync($"/api/images/{aliceImage}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
        foreach (var variant in Enum.GetValues<ImageVariant>())
        {
            Assert.True(File.Exists(environment.PathOf(alice.UserId, hash, variant)));
        }
    }

    /// <summary>Case 18.</summary>
    [Fact]
    public async Task An_anonymous_caller_gets_401_on_every_route()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var aliceImage = await ImageTestEnvironment.UploadOkAsync(environment["alice"].Client, "private.jpg", TestImages.Jpeg());
        using var anonymous = environment.Server.CreateClient();

        foreach (var route in ReadRoutes)
        {
            using var response = await anonymous.GetAsync($"/api/images/{aliceImage}{route}", CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }

        using var delete = await IdentityTestServer.SendWithCsrfAsync(
            anonymous,
            HttpMethod.Delete,
            $"/api/images/{aliceImage}");
        using var upload = await ImageTestEnvironment.UploadAsync(anonymous, "photo.jpg", TestImages.Jpeg(seed: 5));

        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, upload.StatusCode);
        Assert.Empty(environment.StagingFiles());
    }

    [Fact]
    public async Task Each_account_has_its_own_image_identifiers_and_neither_reaches_the_other_through_them()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice", "bob");
        var aliceImage = await ImageTestEnvironment.UploadOkAsync(environment["alice"].Client, "a.jpg", TestImages.Jpeg(seed: 1));
        var bobImage = await ImageTestEnvironment.UploadOkAsync(environment["bob"].Client, "b.jpg", TestImages.Jpeg(seed: 2));

        using var aliceReadsBob = await environment["alice"].Client.GetAsync($"/api/images/{bobImage}/original", CancellationToken.None);
        using var bobReadsAlice = await environment["bob"].Client.GetAsync($"/api/images/{aliceImage}/original", CancellationToken.None);
        using var aliceReadsOwn = await environment["alice"].Client.GetAsync($"/api/images/{aliceImage}/original", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, aliceReadsBob.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, bobReadsAlice.StatusCode);
        Assert.Equal(HttpStatusCode.OK, aliceReadsOwn.StatusCode);
    }

    [Fact]
    public async Task A_context_without_an_authenticated_user_sees_no_image_at_all()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        await ImageTestEnvironment.UploadOkAsync(environment["alice"].Client, "private.jpg", TestImages.Jpeg());

        await using var database = environment.OpenDatabase(userId: null);

        Assert.Equal(0, await database.Images.CountAsync());
    }

    private PostgresFixture RequirePostgres()
    {
        ArgumentNullException.ThrowIfNull(postgres);
        return postgres;
    }
}
