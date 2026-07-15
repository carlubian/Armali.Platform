namespace Segaris.Api.Modules.Identity.Security;

internal static class ApiKeyAuthenticationDefaults
{
    /// <summary>
    /// The scheme that authenticates non-browser clients from an API key.
    /// </summary>
    public const string Scheme = "Segaris.ApiKey";

    /// <summary>
    /// The default scheme. It selects the key handler when an <c>Authorization</c>
    /// header is present and the cookie handler otherwise, so cookie behaviour is
    /// reached through exactly the same path as before.
    /// </summary>
    public const string SelectorScheme = "Segaris.SchemeSelector";

    public const string TokenPrefix = "Bearer ";
}
