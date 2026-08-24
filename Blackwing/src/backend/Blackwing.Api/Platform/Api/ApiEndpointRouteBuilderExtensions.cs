namespace Blackwing.Api.Platform.Api;

internal static class ApiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Opens an API route group under <c>/api</c> with a single OpenAPI tag. Every endpoint in
    /// the application goes through here, so the URL shape is enforced in one place instead of
    /// being a convention nobody checks.
    /// </summary>
    public static RouteGroupBuilder MapBlackwingApiGroup(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        var normalizedPrefix = prefix.Trim('/');
        if (normalizedPrefix.Length == 0
            || normalizedPrefix.Any(character =>
                character is not (>= 'a' and <= 'z')
                and not (>= '0' and <= '9')
                and not '-'
                and not '/'))
        {
            throw new ArgumentException(
                "API route prefixes must use lowercase URL-safe segments.",
                nameof(prefix));
        }

        return endpoints.MapGroup($"/api/{normalizedPrefix}")
            .WithTags(tag);
    }
}
