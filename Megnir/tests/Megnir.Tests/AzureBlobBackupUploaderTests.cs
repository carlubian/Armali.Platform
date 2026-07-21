using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Megnir.Backup;
using Megnir.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Megnir.Tests;

public class AzureBlobBackupUploaderTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"megnir-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Upload_creates_container_and_uploads_to_host_prefix_without_overwriting()
    {
        Directory.CreateDirectory(_temporaryDirectory);
        var zipPath = Path.Combine(_temporaryDirectory, "megnir-backup-20260721-120000.zip");
        await File.WriteAllTextAsync(zipPath, "backup");
        var transport = new RecordingTransport(201, 201);
        var uploader = CreateUploader(transport);

        var uploaded = await uploader.UploadAsync(new BackupResult { ZipPath = zipPath, SizeBytes = 6 });

        Assert.Equal("megnir", uploaded.Container);
        Assert.Equal("host-a/megnir-backup-20260721-120000.zip", uploaded.BlobName);
        Assert.Equal(2, transport.Requests.Count);
        Assert.Contains("restype=container", transport.Requests[0].Uri.Query, StringComparison.Ordinal);
        Assert.EndsWith("/megnir/host-a/megnir-backup-20260721-120000.zip", transport.Requests[1].Uri.AbsolutePath, StringComparison.Ordinal);
        Assert.Equal("*", transport.Requests[1].Headers["If-None-Match"]);
    }

    [Fact]
    public async Task Upload_propagates_azure_failure_and_does_not_delete_local_file()
    {
        Directory.CreateDirectory(_temporaryDirectory);
        var zipPath = Path.Combine(_temporaryDirectory, "megnir-backup.zip");
        await File.WriteAllTextAsync(zipPath, "backup");
        var uploader = CreateUploader(new RecordingTransport(201, 412));

        await Assert.ThrowsAsync<RequestFailedException>(() =>
            uploader.UploadAsync(new BackupResult { ZipPath = zipPath, SizeBytes = 6 }));

        Assert.True(File.Exists(zipPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    private static AzureBlobBackupUploader CreateUploader(RecordingTransport transport)
    {
        const string connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=c2VjcmV0;EndpointSuffix=core.windows.net";
        var client = new BlobServiceClient(connectionString, new BlobClientOptions { Transport = transport });
        return new AzureBlobBackupUploader(
            client,
            new ResolvedAzureOptions("megnir", "host-a"),
            NullLogger<AzureBlobBackupUploader>.Instance);
    }

    private sealed class RecordingTransport(params int[] statuses) : HttpPipelineTransport
    {
        private readonly Queue<int> _statuses = new(statuses);
        private readonly HttpClientTransport _requestFactory = new();

        public List<RecordedRequest> Requests { get; } = [];

        public override Request CreateRequest() => _requestFactory.CreateRequest();

        public override void Process(HttpMessage message) => ProcessCore(message);

        public override ValueTask ProcessAsync(HttpMessage message)
        {
            ProcessCore(message);
            return ValueTask.CompletedTask;
        }

        private void ProcessCore(HttpMessage message)
        {
            var headers = message.Request.Headers.ToDictionary(header => header.Name, header => header.Value, StringComparer.OrdinalIgnoreCase);
            Requests.Add(new RecordedRequest(message.Request.Uri.ToUri(), headers));
            message.Response = new TestResponse(_statuses.Dequeue());
        }
    }

    private sealed record RecordedRequest(Uri Uri, IReadOnlyDictionary<string, string> Headers);

    private sealed class TestResponse(int status) : Response
    {
        public override int Status { get; } = status;
        public override string ReasonPhrase => "Test response";
        public override Stream? ContentStream { get; set; } = Stream.Null;
        public override string ClientRequestId { get; set; } = string.Empty;
        public override void Dispose() { }
        protected override bool ContainsHeader(string name) => false;
        protected override bool TryGetHeader(string name, out string value)
        {
            value = string.Empty;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = [];
            return false;
        }

        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];
    }
}
