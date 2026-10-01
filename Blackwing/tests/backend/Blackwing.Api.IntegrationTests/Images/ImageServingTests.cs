using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Blackwing.Api.IntegrationTests.Identity;
using Blackwing.Api.IntegrationTests.Infrastructure;

namespace Blackwing.Api.IntegrationTests.Images;

/// <summary>
/// The three serving endpoints seen from outside: content types, caching headers, conditional
/// requests and the bytes themselves. Acceptance criterion 5.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ImageServingTests(PostgresFixture postgres)
{
    private static readonly string[] Variants = ["thumb", "preview", "original"];

    /// <summary>Case 9.</summary>
    [Fact]
    public async Task The_three_variants_answer_200_with_the_right_content_type()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", TestImages.Jpeg());

        using var thumbnail = await alice.Client.GetAsync($"/api/images/{id}/thumb", CancellationToken.None);
        using var preview = await alice.Client.GetAsync($"/api/images/{id}/preview", CancellationToken.None);
        using var original = await alice.Client.GetAsync($"/api/images/{id}/original", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, thumbnail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        Assert.Equal("image/webp", thumbnail.Content.Headers.ContentType?.MediaType);
        Assert.Equal("image/webp", preview.Content.Headers.ContentType?.MediaType);
        Assert.Equal("image/jpeg", original.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task The_original_is_offered_as_a_download_under_its_own_file_name()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "summer trip.jpg", TestImages.Jpeg());

        using var original = await alice.Client.GetAsync($"/api/images/{id}/original", CancellationToken.None);
        using var preview = await alice.Client.GetAsync($"/api/images/{id}/preview", CancellationToken.None);

        var disposition = original.Content.Headers.ContentDisposition;
        Assert.NotNull(disposition);
        Assert.Equal("summer trip.jpg", (disposition.FileNameStar ?? disposition.FileName)?.Trim('"'));
        Assert.Null(preview.Content.Headers.ContentDisposition?.FileName);
    }

    /// <summary>Case 10.</summary>
    [Theory]
    [InlineData("thumb")]
    [InlineData("preview")]
    [InlineData("original")]
    public async Task Every_variant_carries_a_strong_etag_and_a_private_immutable_cache_policy(string variant)
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", TestImages.Jpeg());

        using var response = await alice.Client.GetAsync($"/api/images/{id}/{variant}", CancellationToken.None);

        Assert.NotNull(response.Headers.ETag);
        Assert.False(response.Headers.ETag.IsWeak);
        var cacheControl = string.Join(", ", response.Headers.GetValues("Cache-Control"));
        Assert.Contains("private", cacheControl, StringComparison.Ordinal);
        Assert.Contains("immutable", cacheControl, StringComparison.Ordinal);
        Assert.DoesNotContain("public", cacheControl, StringComparison.Ordinal);
        Assert.DoesNotContain("no-store", cacheControl, StringComparison.Ordinal);
        Assert.DoesNotContain("no-cache", cacheControl, StringComparison.Ordinal);

        // A renewed session cookie is what used to overwrite the policy above with no-store, and
        // it would be re-sent on every one of the thousands of thumbnails of a gallery.
        Assert.False(response.Headers.Contains("Set-Cookie"), "Image responses must not re-issue the session cookie.");
        Assert.False(response.Headers.Contains("Pragma"));
        Assert.False(response.Content.Headers.Contains("Expires"));
    }

    [Fact]
    public async Task Serving_an_image_still_validates_the_session_so_a_deactivated_account_is_cut_off()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", TestImages.Jpeg());
        using var before = await alice.Client.GetAsync($"/api/images/{id}/thumb", CancellationToken.None);

        using var deactivation = await IdentityTestServer.SendWithCsrfAsync(
            environment.Admin,
            HttpMethod.Post,
            $"/api/admin/users/{alice.UserId}/deactivate");
        using var after = await alice.Client.GetAsync($"/api/images/{id}/thumb", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deactivation.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task The_three_variants_have_three_different_etags_all_derived_from_the_content()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var jpeg = TestImages.Jpeg();
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", jpeg);
        var hash = ImageTestEnvironment.Sha256(jpeg);

        var tags = new List<string>();
        foreach (var variant in Variants)
        {
            using var response = await alice.Client.GetAsync($"/api/images/{id}/{variant}", CancellationToken.None);
            tags.Add(response.Headers.ETag!.Tag);
        }

        Assert.Equal(3, tags.Distinct(StringComparer.Ordinal).Count());
        Assert.All(tags, tag => Assert.Contains(hash, tag, StringComparison.Ordinal));
    }

    /// <summary>Case 11.</summary>
    [Theory]
    [InlineData("thumb")]
    [InlineData("preview")]
    [InlineData("original")]
    public async Task Repeating_a_request_with_if_none_match_answers_304_with_an_empty_body(string variant)
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", TestImages.Jpeg());
        using var first = await alice.Client.GetAsync($"/api/images/{id}/{variant}", CancellationToken.None);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/images/{id}/{variant}");
        request.Headers.IfNoneMatch.Add(first.Headers.ETag!);
        using var second = await alice.Client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
        Assert.Empty(await second.Content.ReadAsByteArrayAsync(CancellationToken.None));
        Assert.Equal(first.Headers.ETag, second.Headers.ETag);
    }

    /// <summary>Case 12; acceptance criterion 3.</summary>
    [Fact]
    public async Task The_original_comes_back_byte_for_byte_as_it_was_uploaded()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var jpeg = TestImages.JpegWithExif(300, 200, orientation: 6, new DateTime(2019, 3, 4, 5, 6, 7));
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", jpeg);

        var downloaded = await alice.Client.GetByteArrayAsync($"/api/images/{id}/original", CancellationToken.None);

        Assert.Equal(jpeg, downloaded);
    }

    [Fact]
    public async Task The_original_supports_range_requests_and_the_derivatives_do_not()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var jpeg = TestImages.Jpeg();
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", jpeg);

        using var originalRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/images/{id}/original");
        originalRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 9);
        using var original = await alice.Client.SendAsync(originalRequest, CancellationToken.None);

        using var thumbnailRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/images/{id}/thumb");
        thumbnailRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 9);
        using var thumbnail = await alice.Client.SendAsync(thumbnailRequest, CancellationToken.None);

        Assert.Equal(HttpStatusCode.PartialContent, original.StatusCode);
        Assert.Equal(jpeg[..10], await original.Content.ReadAsByteArrayAsync(CancellationToken.None));
        Assert.Equal(HttpStatusCode.OK, thumbnail.StatusCode);
    }

    /// <summary>Case 13.</summary>
    [Fact]
    public async Task The_derivatives_decode_and_their_longest_edge_is_the_configured_one()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "big.jpg", TestImages.Jpeg(width: 3200, height: 1600));

        var thumbnail = await alice.Client.GetByteArrayAsync($"/api/images/{id}/thumb", CancellationToken.None);
        var preview = await alice.Client.GetByteArrayAsync($"/api/images/{id}/preview", CancellationToken.None);

        Assert.Equal((400, 200), TestImages.CodecSize(thumbnail));
        Assert.Equal((1600, 800), TestImages.CodecSize(preview));
    }

    /// <summary>Case 14.</summary>
    [Fact]
    public async Task An_original_smaller_than_the_targets_is_not_scaled_up()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "small.png", TestImages.Png(width: 120, height: 90));

        var thumbnail = await alice.Client.GetByteArrayAsync($"/api/images/{id}/thumb", CancellationToken.None);
        var preview = await alice.Client.GetByteArrayAsync($"/api/images/{id}/preview", CancellationToken.None);

        Assert.Equal((120, 90), TestImages.CodecSize(thumbnail));
        Assert.Equal((120, 90), TestImages.CodecSize(preview));
    }

    [Fact]
    public async Task The_metadata_endpoint_describes_the_image_with_its_logical_size()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var jpeg = TestImages.JpegWithExif(200, 100, orientation: 6);
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "turned.jpg", jpeg);

        var body = await alice.Client.GetFromJsonAsync<JsonElement>($"/api/images/{id}", CancellationToken.None);

        Assert.Equal(id, body.GetProperty("id").GetInt32());
        Assert.Equal("turned.jpg", body.GetProperty("originalFileName").GetString());
        Assert.Equal("image/jpeg", body.GetProperty("contentType").GetString());
        Assert.Equal(jpeg.Length, body.GetProperty("byteSize").GetInt64());
        Assert.Equal(100, body.GetProperty("width").GetInt32());
        Assert.Equal(200, body.GetProperty("height").GetInt32());
        Assert.Equal("pending", body.GetProperty("reviewState").GetString());
        Assert.False(body.TryGetProperty("contentHash", out _), "The hash is an internal detail and is not exposed.");
    }

    [Fact]
    public async Task An_identifier_that_does_not_exist_answers_404_on_every_route()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];

        foreach (var route in new[] { "", "/thumb", "/preview", "/original" })
        {
            using var response = await alice.Client.GetAsync($"/api/images/999999{route}", CancellationToken.None);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task A_row_whose_file_has_gone_missing_answers_503_and_not_404()
    {
        using var environment = await ImageTestEnvironment.StartAsync(RequirePostgres(), "alice");
        var alice = environment["alice"];
        var jpeg = TestImages.Jpeg();
        var id = await ImageTestEnvironment.UploadOkAsync(alice.Client, "photo.jpg", jpeg);
        var hash = ImageTestEnvironment.Sha256(jpeg);
        File.Delete(environment.PathOf(alice.UserId, hash, Blackwing.Shared.Images.ImageVariant.Preview));

        using var response = await alice.Client.GetAsync($"/api/images/{id}/preview", CancellationToken.None);

        // The identifier is valid and belongs to the caller: a missing file is the volume and the
        // database disagreeing, which is the server's problem, not an absent resource.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("image.storage_unavailable", await ImageTestEnvironment.ReadCodeAsync(response));
    }

    private PostgresFixture RequirePostgres()
    {
        ArgumentNullException.ThrowIfNull(postgres);
        return postgres;
    }
}
