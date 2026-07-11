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
/// Drives the gallery and image delivery over HTTP: an authorized owner browses
/// their collection and streams derivatives with strong caching and range
/// support, while another account is refused every byte.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class GalleryDeliveryTests(PostgresFixture fixture)
{
    private const string Password = "Test-Password-1";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Owner_browses_the_gallery_and_streams_cached_derivatives()
    {
        if (!fixture.Available) return;
        var client = await SignInNewUserAsync();
        var imageId = await UploadAndProcessAsync(client, MagickFormat.Jpeg, "image/jpeg");

        // The freshly imported image shows up in the gallery, pending review.
        var page = await client.GetFromJsonAsync<GalleryPageDto>("/api/images/");
        var item = Assert.Single(page!.Items);
        Assert.Equal(imageId, item.Id);
        Assert.False(item.Reviewed);

        // Thumbnail: WebP, strong ETag, private immutable caching.
        var thumb = await client.GetAsync($"/api/images/{imageId}/thumb");
        Assert.Equal(HttpStatusCode.OK, thumb.StatusCode);
        Assert.Equal("image/webp", thumb.Content.Headers.ContentType!.MediaType);
        Assert.NotNull(thumb.Headers.ETag);
        var cacheControl = thumb.Headers.CacheControl!.ToString();
        Assert.Contains("private", cacheControl);
        Assert.Contains("immutable", cacheControl);

        // A conditional request with the same ETag is answered 304, no body re-sent.
        var conditional = new HttpRequestMessage(HttpMethod.Get, $"/api/images/{imageId}/thumb");
        conditional.Headers.IfNoneMatch.Add(thumb.Headers.ETag!);
        var notModified = await client.SendAsync(conditional);
        Assert.Equal(HttpStatusCode.NotModified, notModified.StatusCode);
    }

    [Fact]
    public async Task The_original_is_a_range_capable_download()
    {
        if (!fixture.Available) return;
        var client = await SignInNewUserAsync();
        var imageId = await UploadAndProcessAsync(client, MagickFormat.Jpeg, "image/jpeg");

        var original = await client.GetAsync($"/api/images/{imageId}/original");
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        Assert.Equal("image/jpeg", original.Content.Headers.ContentType!.MediaType);
        Assert.Equal("attachment", original.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal("bytes", original.Headers.AcceptRanges.Single());

        // A range request returns just the asked-for slice.
        var ranged = new HttpRequestMessage(HttpMethod.Get, $"/api/images/{imageId}/original");
        ranged.Headers.Range = new RangeHeaderValue(0, 9);
        var partial = await client.SendAsync(ranged);
        Assert.Equal(HttpStatusCode.PartialContent, partial.StatusCode);
        Assert.NotNull(partial.Content.Headers.ContentRange);
        Assert.Equal(10, (await partial.Content.ReadAsByteArrayAsync()).Length);
    }

    [Fact]
    public async Task Another_account_cannot_reach_the_owners_image()
    {
        if (!fixture.Available) return;
        var owner = await SignInNewUserAsync();
        var imageId = await UploadAndProcessAsync(owner, MagickFormat.Png, "image/png");

        var intruder = await SignInNewUserAsync();
        // Ownership is checked before any byte is served: the id simply does not exist for them.
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/images/{imageId}/thumb")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/images/{imageId}/original")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/images/{imageId}")).StatusCode);
        Assert.Empty((await intruder.GetFromJsonAsync<GalleryPageDto>("/api/images/"))!.Items);
    }

    [Fact]
    public async Task Delivery_requires_authentication()
    {
        if (!fixture.Available) return;
        var anonymous = fixture.Factory!.CreateClient(new() { AllowAutoRedirect = false });
        var response = await anonymous.GetAsync($"/api/images/{Guid.NewGuid()}/thumb");
        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Found);
    }

    // --- helpers ---

    private async Task<HttpClient> SignInNewUserAsync()
    {
        var username = $"user-{Guid.NewGuid():N}";
        await using (var scope = fixture.Factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<BlackwingUser>>();
            var user = new BlackwingUser { Id = Guid.NewGuid(), UserName = username };
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
            await users.AddToRoleAsync(user, BlackwingRoles.User);
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

    private async Task<Guid> UploadAndProcessAsync(HttpClient client, MagickFormat format, string contentType)
    {
        using var image = new MagickImage(MagickColors.CornflowerBlue, 64, 48) { Format = format };
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
    private sealed record UploadFileDto(Guid? JobId, string Status);
    private sealed record UploadBatchDto(IReadOnlyList<UploadFileDto> Files);
    private sealed record UploadJobDto(Guid Id, string Status, Guid? ImageId);
    private sealed record UploadJobListDto(IReadOnlyList<UploadJobDto> Jobs);
}
