using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Blackwing.Api.Ingestion;
using Blackwing.Persistence;
using Blackwing.Persistence.Identity;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Storage;
using ImageMagick;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Blackwing.Api.Tests;

/// <summary>
/// Drives ingestion end to end over HTTP: authenticate, upload real image bytes, and
/// let the background worker turn them into a pending-review image with derivatives.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ImageIngestionTests(PostgresFixture fixture)
{
    private const string Password = "Test-Password-1";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Uploading_an_image_ingests_it_as_a_pending_review_image()
    {
        if (!fixture.Available) return;
        var (client, userId) = await SignInNewUserAsync();
        var bytes = CreateImage(MagickFormat.Jpeg);
        var sha = Sha256(bytes);

        var batch = await UploadAsync(client, "photo.jpg", "image/jpeg", bytes);
        var file = Assert.Single(batch.Files);
        Assert.Equal("accepted", file.Status);
        Assert.NotNull(file.JobId);

        var job = await WaitForTerminalAsync(client, file.JobId!.Value);
        Assert.Equal("Completed", job.Status);
        Assert.NotNull(job.ImageId);

        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        var image = await database.Images.SingleAsync(value => value.Id == job.ImageId);
        Assert.Equal(userId, image.OwnerUserId);
        Assert.Equal(sha, image.Sha256);
        Assert.Equal("image/jpeg", image.ContentType);
        Assert.Equal(64, image.Width);
        Assert.Equal(48, image.Height);
        Assert.Null(image.ReviewedAt); // Fresh imports land pending review.

        var store = scope.ServiceProvider.GetRequiredService<IImageStore>();
        Assert.True(await store.ExistsAsync(userId, sha, ImageDerivative.Original));
        Assert.True(await store.ExistsAsync(userId, sha, ImageDerivative.Preview));
        Assert.True(await store.ExistsAsync(userId, sha, ImageDerivative.Thumbnail));
    }

    [Fact]
    public async Task Re_uploading_the_same_bytes_is_reported_as_a_duplicate()
    {
        if (!fixture.Available) return;
        var (client, _) = await SignInNewUserAsync();
        var bytes = CreateImage(MagickFormat.Png);

        var first = await UploadAsync(client, "a.png", "image/png", bytes);
        var job = await WaitForTerminalAsync(client, first.Files.Single().JobId!.Value);
        Assert.Equal("Completed", job.Status);

        // The same bytes are recognised at upload time and never re-queued.
        var second = await UploadAsync(client, "a-again.png", "image/png", bytes);
        var file = Assert.Single(second.Files);
        Assert.Equal("duplicate", file.Status);
        Assert.Null(file.JobId);
    }

    [Fact]
    public async Task A_non_image_file_is_rejected_without_creating_a_job()
    {
        if (!fixture.Available) return;
        var (client, _) = await SignInNewUserAsync();
        var before = (await ListJobsAsync(client)).Count;

        var batch = await UploadAsync(client, "notes.txt", "text/plain", "this is not an image"u8.ToArray());
        var file = Assert.Single(batch.Files);
        Assert.Equal("rejected", file.Status);
        Assert.Equal(UploadEndpoints.ReasonUnsupportedFormat, file.Reason);

        Assert.Equal(before, (await ListJobsAsync(client)).Count); // No job was created.
    }

    [Fact]
    public async Task Two_identical_files_in_one_batch_yield_one_image_and_one_duplicate()
    {
        if (!fixture.Available) return;
        var (client, userId) = await SignInNewUserAsync();
        var bytes = CreateImage(MagickFormat.WebP);
        var sha = Sha256(bytes);

        using var content = new MultipartFormDataContent();
        Add(content, "one.webp", "image/webp", bytes);
        Add(content, "two.webp", "image/webp", bytes);
        var batch = await SendUploadAsync(client, content);
        Assert.All(batch.Files, file => Assert.Equal("accepted", file.Status));

        var outcomes = new List<string>();
        foreach (var file in batch.Files)
            outcomes.Add((await WaitForTerminalAsync(client, file.JobId!.Value)).Status);

        Assert.Contains("Completed", outcomes);
        Assert.Contains("Duplicate", outcomes);

        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
        Assert.Equal(1, await database.Images.CountAsync(image => image.OwnerUserId == userId && image.Sha256 == sha));
    }

    // --- helpers ---

    private static byte[] CreateImage(MagickFormat format, uint width = 64, uint height = 48)
    {
        using var image = new MagickImage(MagickColors.CornflowerBlue, width, height);
        image.Format = format;
        return image.ToByteArray();
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private async Task<(HttpClient Client, Guid UserId)> SignInNewUserAsync()
    {
        var username = $"user-{Guid.NewGuid():N}";
        Guid userId;
        await using (var scope = fixture.Factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<BlackwingUser>>();
            var user = new BlackwingUser { Id = Guid.NewGuid(), UserName = username };
            var created = await users.CreateAsync(user, Password);
            Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));
            await users.AddToRoleAsync(user, BlackwingRoles.User);
            userId = user.Id;
        }

        var client = fixture.Factory!.CreateClient();
        var token = await CsrfAsync(client);
        var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { username, password = Password }),
        };
        login.Headers.Add("X-CSRF-TOKEN", token);
        var response = await client.SendAsync(login);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return (client, userId);
    }

    private static async Task<string> CsrfAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<AntiforgeryDto>("/api/auth/antiforgery");
        return response!.RequestToken;
    }

    private static void Add(MultipartFormDataContent content, string fileName, string contentType, byte[] bytes)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "files", fileName);
    }

    private static async Task<UploadBatchResponse> SendUploadAsync(HttpClient client, MultipartFormDataContent content)
    {
        var token = await CsrfAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/images/uploads") { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UploadBatchResponse>())!;
    }

    private static async Task<UploadBatchResponse> UploadAsync(HttpClient client, string fileName, string contentType, byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        Add(content, fileName, contentType, bytes);
        return await SendUploadAsync(client, content);
    }

    private static async Task<IReadOnlyList<UploadJobView>> ListJobsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<UploadJobListResponse>("/api/images/uploads"))!.Jobs;

    private async Task<UploadJobView> WaitForTerminalAsync(HttpClient client, Guid jobId)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            var job = (await ListJobsAsync(client)).FirstOrDefault(value => value.Id == jobId);
            if (job is not null && job.Status is "Completed" or "Duplicate" or "Failed") return job;
            await Task.Delay(150);
        }

        throw new TimeoutException($"Upload job {jobId} did not reach a terminal state in time.");
    }

    private sealed record AntiforgeryDto(string RequestToken);
}
