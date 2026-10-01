using System.Net;
using Blackwing.Api.IntegrationTests.Identity;
using Blackwing.Api.IntegrationTests.Infrastructure;
using Blackwing.Persistence;
using Blackwing.Persistence.Content;
using Blackwing.Shared.Content;
using Blackwing.Shared.Images;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blackwing.Api.IntegrationTests.Images;

/// <summary>
/// The relational model: cascades, uniqueness and the ownership of tags and their links. There is
/// no tag endpoint yet (phase 5), so these tests write through a database context bound to one
/// account, which is exactly the path an endpoint will take.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ContentModelTests(PostgresFixture postgres)
{
    private const string TagUniqueIndex = "IX_tags_owner_kind_normalized_value";

    /// <summary>Case 19; acceptance criterion 9.</summary>
    [Fact]
    public async Task Deleting_an_image_removes_its_row_its_tag_links_and_all_three_files_but_keeps_the_tag()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var jpeg = TestImages.Jpeg();
        var hash = ImageTestEnvironment.Sha256(jpeg);
        var imageId = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", jpeg);
        await LinkAsync(environment, alice, imageId, TagKind.Place, "Peñíscola");
        Assert.All(Enum.GetValues<ImageVariant>(), variant => Assert.True(File.Exists(environment.PathOf(alice.UserId, hash, variant))));

        using var response = await IdentityTestServer.SendWithCsrfAsync(
            alice.Client,
            HttpMethod.Delete,
            $"/api/images/{imageId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var database = environment.OpenDatabase(alice);
        Assert.Equal(0, await database.Images.CountAsync());
        Assert.Equal(0, await database.ImageTags.CountAsync());
        Assert.Equal(1, await database.Tags.CountAsync());
        Assert.All(Enum.GetValues<ImageVariant>(), variant => Assert.False(File.Exists(environment.PathOf(alice.UserId, hash, variant))));
        Assert.Empty(environment.StoredFiles());
    }

    [Fact]
    public async Task A_deleted_image_can_be_uploaded_again()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var jpeg = TestImages.Jpeg();
        var firstId = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", jpeg);
        using var delete = await IdentityTestServer.SendWithCsrfAsync(alice.Client, HttpMethod.Delete, $"/api/images/{firstId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var secondId = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", jpeg);

        using var served = await alice.Client.GetAsync($"/api/images/{secondId}/original", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(jpeg, await served.Content.ReadAsByteArrayAsync(CancellationToken.None));
        Assert.Equal(3, environment.StoredFiles().Length);
    }

    [Fact]
    public async Task Deleting_an_image_one_account_shares_bytes_with_leaves_the_other_accounts_files_alone()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice", "bob");
        var jpeg = TestImages.Jpeg();
        var hash = ImageTestEnvironment.Sha256(jpeg);
        var aliceImage = await ImageTestEnvironment.UploadOkAsync(environment["alice"].Client, "photo.jpg", jpeg);
        await ImageTestEnvironment.UploadOkAsync(environment["bob"].Client, "photo.jpg", jpeg);

        using var response = await IdentityTestServer.SendWithCsrfAsync(
            environment["alice"].Client,
            HttpMethod.Delete,
            $"/api/images/{aliceImage}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.All(Enum.GetValues<ImageVariant>(), variant => Assert.False(File.Exists(environment.PathOf(environment["alice"].UserId, hash, variant))));
        Assert.All(Enum.GetValues<ImageVariant>(), variant => Assert.True(File.Exists(environment.PathOf(environment["bob"].UserId, hash, variant))));
    }

    /// <summary>Case 20.</summary>
    [Fact]
    public async Task Deleting_a_tag_removes_only_its_links_and_never_the_images()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var imageId = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", TestImages.Jpeg());
        await LinkAsync(environment, alice, imageId, TagKind.Person, "Antonio");

        await using (var database = environment.OpenDatabase(alice))
        {
            var tag = await database.Tags.SingleAsync();
            database.Tags.Remove(tag);
            await database.SaveChangesAsync();
        }

        await using var verify = environment.OpenDatabase(alice);
        Assert.Equal(1, await verify.Images.CountAsync());
        Assert.Equal(0, await verify.Tags.CountAsync());
        Assert.Equal(0, await verify.ImageTags.CountAsync());
        using var stillServed = await alice.Client.GetAsync($"/api/images/{imageId}/original", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, stillServed.StatusCode);
    }

    /// <summary>Case 21.</summary>
    [Fact]
    public async Task Two_accounts_can_hold_a_tag_of_the_same_kind_and_normalized_value_and_they_are_different_tags()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice", "bob");
        var alice = environment["alice"];
        var bob = environment["bob"];

        int aliceTagId;
        int bobTagId;
        await using (var aliceDatabase = environment.OpenDatabase(alice))
        {
            var tag = NewTag(TagKind.Place, "Peñíscola");
            aliceDatabase.Tags.Add(tag);
            await aliceDatabase.SaveChangesAsync();
            aliceTagId = tag.Id;
            Assert.Equal(alice.UserId, tag.OwnerUserId);
        }

        await using (var bobDatabase = environment.OpenDatabase(bob))
        {
            var tag = NewTag(TagKind.Place, "peniscola");
            bobDatabase.Tags.Add(tag);
            await bobDatabase.SaveChangesAsync();
            bobTagId = tag.Id;
            Assert.Equal(bob.UserId, tag.OwnerUserId);
        }

        Assert.NotEqual(aliceTagId, bobTagId);
    }

    /// <summary>Case 22.</summary>
    [Fact]
    public async Task The_same_account_cannot_hold_two_tags_of_the_same_kind_and_normalized_value()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        await using var database = environment.OpenDatabase(alice);
        database.Tags.Add(NewTag(TagKind.Place, "Peñíscola"));
        await database.SaveChangesAsync();

        // Same kind, and a spelling that normalizes to the same key.
        database.Tags.Add(NewTag(TagKind.Place, "  peniscola "));
        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());

        var postgresError = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresError.SqlState);
        Assert.Equal(TagUniqueIndex, postgresError.ConstraintName);
    }

    [Fact]
    public async Task The_same_value_under_a_different_kind_is_a_different_tag()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        await using var database = environment.OpenDatabase(alice);

        database.Tags.Add(NewTag(TagKind.Person, "Navidad"));
        database.Tags.Add(NewTag(TagKind.Topic, "Navidad"));
        await database.SaveChangesAsync();

        Assert.Equal(2, await database.Tags.CountAsync());
    }

    /// <summary>Case 23.</summary>
    [Fact]
    public async Task Each_account_sees_only_its_own_tags_and_its_own_associations()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice", "bob");
        var alice = environment["alice"];
        var bob = environment["bob"];
        var aliceImage = await ImageTestEnvironment.UploadOkAsync(alice.Client, "a.jpg", TestImages.Jpeg(seed: 1));
        var bobImage = await ImageTestEnvironment.UploadOkAsync(bob.Client, "b.jpg", TestImages.Jpeg(seed: 2));
        await LinkAsync(environment, alice, aliceImage, TagKind.Person, "Antonio");
        await LinkAsync(environment, alice, aliceImage, TagKind.Topic, "Vacaciones");
        await LinkAsync(environment, bob, bobImage, TagKind.Person, "Antonio");

        await using var aliceDatabase = environment.OpenDatabase(alice);
        await using var bobDatabase = environment.OpenDatabase(bob);

        Assert.Equal(2, await aliceDatabase.Tags.CountAsync());
        Assert.Equal(2, await aliceDatabase.ImageTags.CountAsync());
        Assert.Equal(1, await bobDatabase.Tags.CountAsync());
        Assert.Equal(1, await bobDatabase.ImageTags.CountAsync());
        Assert.All(await aliceDatabase.ImageTags.ToListAsync(), link => Assert.Equal(alice.UserId, link.OwnerUserId));
        Assert.All(await bobDatabase.Tags.ToListAsync(), tag => Assert.Equal(bob.UserId, tag.OwnerUserId));
    }

    [Fact]
    public async Task Owned_content_cannot_be_created_without_an_authenticated_user()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres());
        await using var database = environment.OpenDatabase(userId: null);

        database.Tags.Add(NewTag(TagKind.Place, "Nowhere"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => database.SaveChangesAsync());
    }

    [Fact]
    public async Task A_link_to_an_image_that_does_not_exist_is_rejected_by_the_database()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        await using var database = environment.OpenDatabase(alice);
        var tag = NewTag(TagKind.Topic, "Orphan");
        database.Tags.Add(tag);
        await database.SaveChangesAsync();

        database.ImageTags.Add(new ImageTag { ImageId = 424242, TagId = tag.Id, AssignedAt = DateTimeOffset.UtcNow });

        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(failure.InnerException).SqlState);
    }

    private static Tag NewTag(TagKind kind, string value) => new()
    {
        Kind = kind,
        Value = value.Trim(),
        NormalizedValue = TagNormalizer.Normalize(value),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>Creates the tag if the account lacks it and links it to the image.</summary>
    private static async Task LinkAsync(
        ImageTestEnvironment environment,
        TestAccount account,
        int imageId,
        TagKind kind,
        string value)
    {
        await using var database = environment.OpenDatabase(account);

        var normalized = TagNormalizer.Normalize(value);
        var tag = await database.Tags.SingleOrDefaultAsync(
            candidate => candidate.Kind == kind && candidate.NormalizedValue == normalized);
        if (tag is null)
        {
            tag = NewTag(kind, value);
            database.Tags.Add(tag);
            await database.SaveChangesAsync();
        }

        database.ImageTags.Add(new ImageTag { ImageId = imageId, TagId = tag.Id, AssignedAt = DateTimeOffset.UtcNow });
        await database.SaveChangesAsync();
    }

    private PostgresFixture RequirePostgres()
    {
        ArgumentNullException.ThrowIfNull(postgres);
        return postgres;
    }
}
