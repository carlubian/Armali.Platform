namespace Segaris.Api.Platform.Api;

internal static class ApiEndpointRouteBuilderExtensions
{
    public static TBuilder WithRequestBodyLimit<TBuilder>(this TBuilder builder, long maximumBytes)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.WithMetadata(new ApiRequestBodyLimit(maximumBytes));
    }

    public static RouteGroupBuilder MapSegarisApiGroup(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        string tag)
    {
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
