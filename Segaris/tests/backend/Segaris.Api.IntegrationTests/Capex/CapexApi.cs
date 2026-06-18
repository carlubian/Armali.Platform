using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Segaris.Api.IntegrationTests.Capex;

/// <summary>
/// Thin helpers for exercising the Capex mutation and attachment routes with the
/// antiforgery token pair the production cookie flow requires.
/// </summary>
internal static class CapexApi
{
    public static async Task<HttpResponseMessage> PostJsonAsync<T>(
        HttpClient client,
        string route,
        T body,
        string? csrf)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = JsonContent.Create(body),
        };
        AddCsrf(request, csrf);
        return await client.SendAsync(request, CancellationToken.None);
    }

    public static async Task<HttpResponseMessage> PutJsonAsync<T>(
        HttpClient client,
        string route,
        T body,
        string? csrf)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, route)
        {
            Content = JsonContent.Create(body),
        };
        AddCsrf(request, csrf);
        return await client.SendAsync(request, CancellationToken.None);
    }

    public static async Task<HttpResponseMessage> DeleteAsync(
        HttpClient client,
        string route,
        string? csrf)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, route);
        AddCsrf(request, csrf);
        return await client.SendAsync(request, CancellationToken.None);
    }

    public static async Task<HttpResponseMessage> PutAsync(
        HttpClient client,
        string route,
        string? csrf)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, route);
        AddCsrf(request, csrf);
        return await client.SendAsync(request, CancellationToken.None);
    }

    public static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        string route,
        string fileName,
        string contentType,
        byte[] content,
        string? csrf)
    {
        using var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(file, "file", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = multipart,
        };
        AddCsrf(request, csrf);
        return await client.SendAsync(request, CancellationToken.None);
    }

    private static void AddCsrf(HttpRequestMessage request, string? csrf)
    {
        if (csrf is not null)
        {
            request.Headers.Add("X-CSRF-TOKEN", csrf);
        }
    }
}
