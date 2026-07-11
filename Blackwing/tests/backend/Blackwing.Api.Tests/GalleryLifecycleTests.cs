using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blackwing.Persistence.Identity;
using Blackwing.Shared.Identity;
using ImageMagick;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Blackwing.Api.Tests;

/// <summary>
/// End-to-end coverage over HTTP of a user managing their own collection: the full
/// review → tag → filter → merge → delete cycle, the observability contract every
/// response must honour, and the admin-only operations surface. A second account is
/// present throughout to prove owner isolation at every step.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class GalleryLifecycleTests(PostgresFixture fixture)
{
    private const string Password = "Test-Password-1";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Review_tag_filter_and_delete_cycle_stays_owner_scoped()
    {
        if (!fixture.Available) return;
        var owner = await SignInNewUserAsync();
        var intruder = await SignInNewUserAsync();
        var imageId = await UploadAndProcessAsync(owner, MagickFormat.Jpeg, "image/jpeg");

        // Freshly ingested: pending review, visible under the pending filter only.
        var pending = await owner.GetFromJsonAsync<GalleryPageDto>("/api/images/?status=Pending");
        Assert.Contains(pending!.Items, item => item.Id == imageId && !item.Reviewed);
        var review = await owner.GetFromJsonAsync<ReviewResponseDto>("/api/images/review");
        Assert.True(review!.PendingCount >= 1);
        Assert.Equal(imageId, review.Image!.Id);

        // Tag it and complete the review in one call.
        await PutTagsAsync(owner, imageId, markReviewed: true, ("Person", "Alice"));

        // Now reviewed: gone from pending, present under reviewed, carrying the tag.
        Assert.DoesNotContain((await owner.GetFromJsonAsync<GalleryPageDto>("/api/images/?status=Pending"))!.Items, item => item.Id == imageId);
        Assert.Contains((await owner.GetFromJsonAsync<GalleryPageDto>("/api/images/?status=Reviewed"))!.Items, item => item.Id == imageId);
        var view = await owner.GetFromJsonAsync<ImageViewDto>($"/api/images/{imageId}");
        var aliceTag = Assert.Single(view!.Tags);
        Assert.Equal("Alice", aliceTag.Value);

        // The tag drives both the filter and the autocomplete.
        Assert.Contains((await owner.GetFromJsonAsync<GalleryPageDto>($"/api/images/?tag={aliceTag.Id}"))!.Items, item => item.Id == imageId);
        Assert.Contains((await owner.GetFromJsonAsync<TagListDto>("/api/tags/?type=Person&query=Al"))!.Tags, tag => tag.Id == aliceTag.Id);

        // The intruder can neither read nor mutate the owner's image or tag.
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/images/{imageId}")).StatusCode);
        Assert.Empty((await intruder.GetFromJsonAsync<GalleryPageDto>("/api/images/"))!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await PutTagsResponseAsync(intruder, imageId, markReviewed: false, ("Person", "Mallory"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync(intruder, $"/api/images/{imageId}")).StatusCode);

        // The owner deletes the image; it vanishes and its now-orphaned tag with it.
        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync(owner, $"/api/images/{imageId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/images/{imageId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/images/{imageId}/thumb")).StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<TagListDto>("/api/tags/?type=Person&query=Al"))!.Tags);
    }

    [Fact]
    public async Task Merging_two_tags_repoints_images_and_removes_the_source()
    {
        if (!fixture.Available) return;
        var owner = await SignInNewUserAsync();
        var intruder = await SignInNewUserAsync();
        var first = await UploadAndProcessAsync(owner, MagickFormat.Jpeg, "image/jpeg");
        var second = await UploadAndProcessAsync(owner, MagickFormat.Png, "image/png");

        await PutTagsAsync(owner, first, markReviewed: true, ("Person", "Alice"));
        await PutTagsAsync(owner, second, markReviewed: true, ("Person", "Alicia"));
        var source = (await owner.GetFromJsonAsync<ImageViewDto>($"/api/images/{first}"))!.Tags.Single();   // Alice
        var target = (await owner.GetFromJsonAsync<ImageViewDto>($"/api/images/{second}"))!.Tags.Single();  // Alicia

        // The intruder cannot merge tags they do not own.
        Assert.Equal(HttpStatusCode.NotFound, (await MergeResponseAsync(intruder, source.Id, target.Id)).StatusCode);

        // The owner merges Alice into Alicia: the source disappears and its image repoints.
        Assert.Equal(HttpStatusCode.NoContent, (await MergeResponseAsync(owner, source.Id, target.Id)).StatusCode);
        var remaining = (await owner.GetFromJsonAsync<TagListDto>("/api/tags/?type=Person&query=Al"))!.Tags;
        Assert.DoesNotContain(remaining, tag => tag.Id == source.Id);
        Assert.Contains(remaining, tag => tag.Id == target.Id);
        var firstTags = (await owner.GetFromJsonAsync<ImageViewDto>($"/api/images/{first}"))!.Tags;
        Assert.Equal(target.Id, Assert.Single(firstTags).Id);
    }

    [Fact]
    public async Task Every_response_is_traceable_and_declines_content_sniffing()
    {
        if (!fixture.Available) return;
        var owner = await SignInNewUserAsync();

        // A successful response carries the correlation id and the nosniff guard.
        var ok = await owner.GetAsync("/api/images/");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.NotEmpty(TraceHeader(ok));
        Assert.Equal("nosniff", ok.Headers.TryGetValues("X-Content-Type-Options", out var values) ? values.Single() : null);

        // An error response is ProblemDetails whose traceId matches the header, so a
        // user-reported failure can be tied back to the logs.
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/images/{Guid.NewGuid()}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var notFound = await owner.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Contains("problem+json", notFound.Content.Headers.ContentType!.MediaType);
        var problem = await notFound.Content.ReadFromJsonAsync<ProblemDto>();
        Assert.Equal(TraceHeader(notFound), problem!.TraceId);
    }

    [Fact]
    public async Task The_ingestion_queue_snapshot_is_admin_only()
    {
        if (!fixture.Available) return;
        var user = await SignInNewUserAsync();
        var admin = await SignInNewUserAsync(admin: true);
        await UploadAndProcessAsync(user, MagickFormat.Jpeg, "image/jpeg");

        // Anonymous and non-admin callers are refused the operations surface.
        var anonymous = fixture.Factory!.CreateClient(new() { AllowAutoRedirect = false });
        Assert.True((await anonymous.GetAsync("/api/ops/ingestion")).StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Found);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/ops/ingestion")).StatusCode);

        // The admin sees aggregate queue health, reflecting the just-completed ingestion.
        var snapshot = await admin.GetFromJsonAsync<IngestionQueueDto>("/api/ops/ingestion");
        Assert.NotNull(snapshot);
        Assert.True(snapshot!.Completed >= 1);
    }

    // --- helpers ---

    private async Task<HttpClient> SignInNewUserAsync(bool admin = false)
    {
        var username = $"user-{Guid.NewGuid():N}";
        await using (var scope = fixture.Factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<BlackwingUser>>();
            var user = new BlackwingUser { Id = Guid.NewGuid(), UserName = username };
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
            await users.AddToRoleAsync(user, admin ? BlackwingRoles.Admin : BlackwingRoles.User);
        }

        var client = fixture.Factory!.CreateClient();
        var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { username, password = Password }),
        };
        login.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(login)).StatusCode);
        return client;
    }

    private static async Task<string> CsrfAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<AntiforgeryDto>("/api/auth/antiforgery"))!.RequestToken;

    private static string TraceHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-Trace-ID", out var values) ? values.Single() : string.Empty;

    private Task PutTagsAsync(HttpClient client, Guid imageId, bool markReviewed, params (string Type, string Value)[] tags) =>
        AssertNoContentAsync(PutTagsResponseAsync(client, imageId, markReviewed, tags));

    private async Task<HttpResponseMessage> PutTagsResponseAsync(HttpClient client, Guid imageId, bool markReviewed, params (string Type, string Value)[] tags)
    {
        var body = new { tags = tags.Select(tag => new { type = tag.Type, value = tag.Value }).ToArray(), markReviewed };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/images/{imageId}/tags") { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> MergeResponseAsync(HttpClient client, Guid sourceId, Guid targetId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/tags/{sourceId}/merge") { Content = JsonContent.Create(new { targetTagId = targetId }) };
        request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteAsync(HttpClient client, string uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private static async Task AssertNoContentAsync(Task<HttpResponseMessage> pending) =>
        Assert.Equal(HttpStatusCode.NoContent, (await pending).StatusCode);

    private async Task<Guid> UploadAndProcessAsync(HttpClient client, MagickFormat format, string contentType)
    {
        // A unique colour per upload keeps the SHA-256 distinct, so a second image is
        // never rejected as a per-user duplicate of the first.
        using var image = new MagickImage(new MagickColor((byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256)), 64, 48) { Format = format };
        var bytes = image.ToByteArray();

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "files", $"photo.{format}".ToLowerInvariant());

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/images/uploads") { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", await CsrfAsync(client));
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var batch = (await response.Content.ReadFromJsonAsync<UploadBatchDto>())!;
        var jobId = batch.Files.Single().JobId!.Value;

        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            var jobs = (await client.GetFromJsonAsync<UploadJobListDto>("/api/images/uploads"))!.Jobs;
            var job = jobs.FirstOrDefault(value => value.Id == jobId);
            if (job?.Status == "Completed") return job.ImageId!.Value;
            if (job?.Status is "Failed" or "Duplicate") throw new InvalidOperationException($"Unexpected job status {job.Status}.");
            await Task.Delay(150);
        }

        throw new TimeoutException("Upload did not complete in time.");
    }

    private sealed record AntiforgeryDto(string RequestToken);
    private sealed record GalleryItemDto(Guid Id, bool Reviewed);
    private sealed record GalleryPageDto(IReadOnlyList<GalleryItemDto> Items, string? NextCursor);
    private sealed record TagDto(Guid Id, string Type, string Value);
    private sealed record TagListDto(IReadOnlyList<TagDto> Tags);
    private sealed record ImageViewDto(Guid Id, IReadOnlyList<TagDto> Tags);
    private sealed record ReviewResponseDto(int PendingCount, ImageViewDto? Image);
    private sealed record ProblemDto(string? TraceId);
    private sealed record IngestionQueueDto(long Pending, long Processing, long Completed, long Failed, long Duplicate, double? OldestPendingAgeSeconds);
    private sealed record UploadFileDto(Guid? JobId, string Status);
    private sealed record UploadBatchDto(IReadOnlyList<UploadFileDto> Files);
    private sealed record UploadJobDto(Guid Id, string Status, Guid? ImageId);
    private sealed record UploadJobListDto(IReadOnlyList<UploadJobDto> Jobs);
}
