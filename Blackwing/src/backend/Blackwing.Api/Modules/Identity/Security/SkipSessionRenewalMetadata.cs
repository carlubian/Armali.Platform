namespace Blackwing.Api.Modules.Identity.Security;

/// <summary>
/// Endpoint metadata marking responses that must not carry a renewed session cookie.
/// </summary>
/// <remarks>
/// The security stamp is validated on every request (the validation interval is zero, so that
/// deactivating an account cuts its sessions at once), and each successful validation asks the
/// cookie handler to re-issue the session cookie. Re-issuing writes <c>Set-Cookie</c> and, as a
/// side effect that cannot be switched off, overwrites the response with
/// <c>Cache-Control: no-store, no-cache</c>, <c>Pragma: no-cache</c> and an expiry in 1970.
///
/// For ordinary API calls that is harmless. For an image it is not: the whole point of serving a
/// thumbnail as <c>private, immutable</c> is that the browser keeps it, and a gallery of tens of
/// thousands of photos that re-downloads every thumbnail on every visit is not usable. The
/// validation itself still happens on these requests; only the re-issue of the cookie is skipped.
/// </remarks>
internal sealed class SkipSessionRenewalMetadata
{
    public static SkipSessionRenewalMetadata Instance { get; } = new();
}
