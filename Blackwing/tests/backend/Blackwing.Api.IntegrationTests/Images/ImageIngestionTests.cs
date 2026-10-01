using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Blackwing.Api.IntegrationTests.Identity;
using Blackwing.Api.IntegrationTests.Infrastructure;
using Blackwing.Shared.Content;
using Blackwing.Shared.Images;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.IntegrationTests.Images;

/// <summary>
/// The path an image takes from the request to the volume and the database, end to end through
/// the real composition root, the real PostgreSQL engine and the real file system.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ImageIngestionTests(PostgresFixture postgres)
{
    /// <summary>Case 1; acceptance criterion 3.</summary>
    [Fact]
    public async Task Uploading_a_jpeg_records_it_pending_and_stores_the_original_intact_with_two_derivatives()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var jpeg = TestImages.Jpeg(width: 64, height: 48);

        using var response = await ImageTestEnvironment.UploadAsync(alice.Client, "holiday.jpg", jpeg);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        var id = body.GetProperty("id").GetInt32();
        Assert.Equal($"/api/images/{id}", response.Headers.Location?.OriginalString);
        Assert.Equal("pending", body.GetProperty("reviewState").GetString());

        await using var database = environment.OpenDatabase(alice);
        var image = await database.Images.SingleAsync();
        var hash = ImageTestEnvironment.Sha256(jpeg);

        Assert.Equal(id, image.Id);
        Assert.Equal(alice.UserId, image.OwnerUserId);
        Assert.Equal(hash, image.ContentHash);
        Assert.Equal(jpeg.Length, image.ByteSize);
        Assert.Equal(64, image.Width);
        Assert.Equal(48, image.Height);
        Assert.Equal("image/jpeg", image.ContentType);
        Assert.Equal("holiday.jpg", image.OriginalFileName);
        Assert.Equal(ImageReviewState.Pending, image.ReviewState);
        Assert.Null(image.ReviewedAt);

        // The three files, at the layout the milestone fixed, and the original byte for byte.
        var original = environment.PathOf(alice.UserId, hash, ImageVariant.Original);
        Assert.Equal(Path.Combine(environment.ImagesRoot, alice.UserId.ToString(), hash[..2], hash[2..4], hash), original);
        Assert.Equal(jpeg, await File.ReadAllBytesAsync(original));
        Assert.True(File.Exists(environment.PathOf(alice.UserId, hash, ImageVariant.Thumbnail)));
        Assert.True(File.Exists(environment.PathOf(alice.UserId, hash, ImageVariant.Preview)));
        Assert.Equal(3, environment.StoredFiles().Length);
        Assert.Empty(environment.StagingFiles());
    }

    /// <summary>Case 2; acceptance criterion 4.</summary>
    [Fact]
    public async Task An_exif_orientation_is_applied_to_the_dimensions_and_derivatives_but_never_to_the_original()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];

        // Stored as 200 x 100 with orientation 6: a quarter turn, so the photo is really 100 x 200.
        var jpeg = TestImages.JpegWithExif(
            width: 200,
            height: 100,
            orientation: 6,
            dateTimeOriginal: new DateTime(2021, 7, 15, 10, 30, 45));
        Assert.Equal((200, 100), TestImages.CodecSize(jpeg));

        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "portrait.jpg", jpeg);

        await using var database = environment.OpenDatabase(alice);
        var image = await database.Images.SingleAsync(candidate => candidate.Id == id);
        var capturedAt = new DateTimeOffset(2021, 7, 15, 10, 30, 45, TimeSpan.Zero);

        Assert.Equal(100, image.Width);
        Assert.Equal(200, image.Height);
        Assert.Equal(capturedAt, image.CapturedAt);
        Assert.Equal(capturedAt, image.SortedAt);

        // The original still carries its EXIF and its stored size.
        var original = await File.ReadAllBytesAsync(environment.PathOf(alice.UserId, image.ContentHash, ImageVariant.Original));
        Assert.Equal(jpeg, original);

        // The derivatives are upright. The red marker began in the top-left of the stored image,
        // so after a clockwise quarter turn it sits in the top-right.
        var preview = await File.ReadAllBytesAsync(environment.PathOf(alice.UserId, image.ContentHash, ImageVariant.Preview));
        Assert.Equal((100, 200), TestImages.CodecSize(preview));
        var marker = TestImages.MarkerSize(200, 100);
        Assert.True(TestImages.IsRed(TestImages.PixelAt(preview, 100 - 1 - (marker / 2), marker / 2)));
        Assert.False(TestImages.IsRed(TestImages.PixelAt(preview, marker / 2, marker / 2)));
    }

    /// <summary>Case 3.</summary>
    [Fact]
    public async Task A_file_without_exif_has_no_capture_date_and_sorts_by_its_upload_date()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];

        await ImageTestEnvironment.UploadOkAsync(alice.Client, "plain.png", TestImages.Png());

        await using var database = environment.OpenDatabase(alice);
        var image = await database.Images.SingleAsync();

        Assert.Null(image.CapturedAt);
        Assert.Equal(image.UploadedAt, image.SortedAt);
    }

    /// <summary>Case 4; acceptance criterion 7, first half.</summary>
    [Fact]
    public async Task Uploading_the_same_file_twice_is_refused_and_leaves_one_row_and_no_extra_files()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var jpeg = TestImages.Jpeg();
        await ImageTestEnvironment.UploadOkAsync(alice.Client, "first.jpg", jpeg);
        var filesAfterFirst = environment.StoredFiles();

        using var second = await ImageTestEnvironment.UploadAsync(alice.Client, "renamed.jpg", jpeg);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("image.duplicate", await ImageTestEnvironment.ReadCodeAsync(second));
        await using var database = environment.OpenDatabase(alice);
        Assert.Equal(1, await database.Images.CountAsync());
        Assert.Equal(filesAfterFirst.Order(StringComparer.Ordinal), environment.StoredFiles().Order(StringComparer.Ordinal));
        Assert.Empty(environment.StagingFiles());
    }

    /// <summary>
    /// Case 5; acceptance criterion 7, second half. Duplicating bytes between accounts is the
    /// milestone's decision, not a defect: privacy is bought with disk.
    /// </summary>
    [Fact]
    public async Task The_same_file_in_two_accounts_creates_two_rows_and_two_copies_on_disk()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice", "bob");
        var alice = environment["alice"];
        var bob = environment["bob"];
        var jpeg = TestImages.Jpeg();
        var hash = ImageTestEnvironment.Sha256(jpeg);

        var aliceId = await ImageTestEnvironment.UploadOkAsync(alice.Client, "shared.jpg", jpeg);
        var bobId = await ImageTestEnvironment.UploadOkAsync(bob.Client, "shared.jpg", jpeg);

        await using (var aliceDatabase = environment.OpenDatabase(alice))
        {
            Assert.Equal(aliceId, (await aliceDatabase.Images.SingleAsync()).Id);
        }

        await using (var bobDatabase = environment.OpenDatabase(bob))
        {
            Assert.Equal(bobId, (await bobDatabase.Images.SingleAsync()).Id);
        }

        Assert.NotEqual(aliceId, bobId);
        foreach (var account in new[] { alice, bob })
        {
            Assert.Equal(jpeg, await File.ReadAllBytesAsync(environment.PathOf(account.UserId, hash, ImageVariant.Original)));
            Assert.True(File.Exists(environment.PathOf(account.UserId, hash, ImageVariant.Thumbnail)));
            Assert.True(File.Exists(environment.PathOf(account.UserId, hash, ImageVariant.Preview)));
        }

        Assert.Equal(6, environment.StoredFiles().Length);
    }

    /// <summary>Case 6.</summary>
    [Fact]
    public async Task A_jpg_whose_bytes_are_a_png_is_refused_and_leaves_nothing_behind()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];

        using var response = await ImageTestEnvironment.UploadAsync(
            alice.Client,
            "fake.jpg",
            TestImages.Png(),
            contentType: "image/jpeg");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("image.content_mismatch", await ImageTestEnvironment.ReadCodeAsync(response));
        await AssertNothingWasKeptAsync(environment, alice);
    }

    [Fact]
    public async Task A_text_file_named_as_an_image_is_refused()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];

        using var response = await ImageTestEnvironment.UploadAsync(alice.Client, "notes.png", TestImages.NotAnImage());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("image.content_mismatch", await ImageTestEnvironment.ReadCodeAsync(response));
        await AssertNothingWasKeptAsync(environment, alice);
    }

    [Fact]
    public async Task A_declared_content_type_that_contradicts_the_extension_is_refused()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];

        using var response = await ImageTestEnvironment.UploadAsync(
            alice.Client,
            "photo.jpg",
            TestImages.Jpeg(),
            contentType: "image/png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("image.content_mismatch", await ImageTestEnvironment.ReadCodeAsync(response));
        await AssertNothingWasKeptAsync(environment, alice);
    }

    [Fact]
    public async Task A_file_with_valid_magic_numbers_and_a_damaged_body_is_refused_as_undecodable()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];

        using var response = await ImageTestEnvironment.UploadAsync(alice.Client, "broken.jpg", TestImages.CorruptJpeg());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("image.undecodable", await ImageTestEnvironment.ReadCodeAsync(response));
        await AssertNothingWasKeptAsync(environment, alice);
    }

    /// <summary>Case 7; acceptance criterion 8.</summary>
    [Fact]
    public async Task A_file_one_byte_over_the_limit_is_refused_with_413_and_leaves_nothing_in_staging()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        const long Limit = 100L * 1024 * 1024;

        // A generated stream: the 100 MB never exists as an array, on either side of the wire.
        using var response = await ImageTestEnvironment.UploadAsync(
            alice.Client,
            "huge.jpg",
            TestImages.Endless(Limit + 1),
            contentType: "image/jpeg");

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("image.too_large", await ImageTestEnvironment.ReadCodeAsync(response));
        await AssertNothingWasKeptAsync(environment, alice);
    }

    /// <summary>Case 8.</summary>
    [Fact]
    public async Task A_format_that_is_not_accepted_is_refused_with_400()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];

        using var response = await ImageTestEnvironment.UploadAsync(
            alice.Client,
            "animation.gif",
            "GIF89a\u0001\u0000\u0001\u0000"u8.ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("image.format_unsupported", await ImageTestEnvironment.ReadCodeAsync(response));
        await AssertNothingWasKeptAsync(environment, alice);
    }

    [Fact]
    public async Task An_owner_smuggled_into_the_form_is_ignored_and_the_image_belongs_to_the_uploader()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice", "bob");
        var alice = environment["alice"];
        var bob = environment["bob"];

        using var response = await ImageTestEnvironment.UploadAsync(
            alice.Client,
            "mine.jpg",
            TestImages.Jpeg(),
            extraFields: [("ownerUserId", bob.UserId.ToString())]);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var aliceDatabase = environment.OpenDatabase(alice);
        await using var bobDatabase = environment.OpenDatabase(bob);
        Assert.Equal(1, await aliceDatabase.Images.CountAsync());
        Assert.Equal(0, await bobDatabase.Images.CountAsync());
    }

    [Fact]
    public async Task An_upload_without_the_antiforgery_token_is_refused()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];

        using var response = await ImageTestEnvironment.UploadAsync(
            alice.Client,
            "photo.jpg",
            TestImages.Jpeg(),
            includeAntiforgery: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNothingWasKeptAsync(environment, alice);
    }

    [Fact]
    public async Task A_request_that_is_not_multipart_is_refused()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];

        using var response = await PostJsonAsync(alice.Client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_multipart_request_without_a_file_part_is_refused()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var csrf = await IdentityTestServer.GetCsrfTokenAsync(alice.Client);

        using var form = new MultipartFormDataContent { { new StringContent("hello"), "comment" } };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/images") { Content = form };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await alice.Client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client)
    {
        var csrf = await IdentityTestServer.GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/images")
        {
            Content = JsonContent.Create(new { file = "nope" }),
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request, CancellationToken.None);
    }

    /// <summary>A refused upload must leave neither a row, nor a stored file, nor a staged one.</summary>
    private static async Task AssertNothingWasKeptAsync(ImageTestEnvironment environment, TestAccount account)
    {
        await using var database = environment.OpenDatabase(account);
        Assert.Equal(0, await database.Images.CountAsync());
        Assert.Empty(environment.StoredFiles());
        Assert.Empty(environment.StagingFiles());
    }

    private PostgresFixture RequirePostgres()
    {
        ArgumentNullException.ThrowIfNull(postgres);
        return postgres;
    }
}
