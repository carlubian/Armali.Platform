using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Attachments;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Identity.Endpoints;

internal static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var profile = endpoints.MapSegarisApiGroup("session/profile", "Profile")
            .RequireAuthorization();

        profile.MapGet("", GetProfileAsync)
            .WithSummary("Returns the current user's profile");
        profile.MapPut("", UpdateProfileAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Updates the current user's display name and language preference");
        profile.MapPut("/avatar", PutAvatarAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + (1024 * 1024))
            .WithSummary("Uploads or replaces the current user's avatar");
        profile.MapGet("/avatar", GetOwnAvatarAsync)
            .WithSummary("Downloads the current user's avatar");
        profile.MapDelete("/avatar", DeleteAvatarAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Removes the current user's avatar");

        endpoints.MapSegarisApiGroup("users", "User avatars")
            .RequireAuthorization()
            .MapGet("/{id:int}/avatar", GetAvatarAsync)
            .WithSummary("Downloads a household user's avatar");
    }

    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, userManager);
        var avatar = await attachments.FindByOwnerAsync(
            IdentityProfilePolicy.AvatarOwner(user.Id),
            cancellationToken);
        return TypedResults.Ok(ToResponse(user, avatar is not null));
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest request,
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var values = IdentityProfilePolicy.Validate(request.DisplayName, request.Language);
        var user = await GetCurrentUserAsync(principal, userManager);
        user.DisplayName = values.DisplayName;
        user.Language = values.Language;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw IdentityProblem.FromResult(result, "displayName");
        }

        var avatar = await attachments.FindByOwnerAsync(
            IdentityProfilePolicy.AvatarOwner(user.Id),
            cancellationToken);
        return TypedResults.Ok(ToResponse(user, avatar is not null));
    }

    private static async Task<IResult> PutAvatarAsync(
        HttpRequest request,
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            throw AttachmentProblem.Invalid("file", "A multipart form file is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            throw AttachmentProblem.Invalid("file", "A multipart form file is required.");
        }

        IdentityProfilePolicy.ValidateAvatar(file.ContentType);
        var user = await GetCurrentUserAsync(principal, userManager);
        var owner = IdentityProfilePolicy.AvatarOwner(user.Id);
        var previous = await attachments.FindByOwnerAsync(owner, cancellationToken);

        await using var stream = file.OpenReadStream();
        var created = await attachments.CreateAsync(
            new(owner, file.FileName, file.ContentType, stream),
            new UserId(user.Id),
            cancellationToken);

        if (previous is not null)
        {
            await attachments.DeleteAsync(previous.Id, owner, cancellationToken);
        }

        return TypedResults.Ok(new AvatarResponse(
            IdentityProfilePolicy.AvatarUrl(user.Id),
            created.ContentType,
            created.Size));
    }

    private static async Task<IResult> GetOwnAvatarAsync(
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, userManager);
        return await StreamAvatarAsync(user.Id, attachments, cancellationToken);
    }

    private static async Task<IResult> GetAvatarAsync(
        int id,
        UserManager<SegarisUser> userManager,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (user is null)
        {
            throw ApiProblemException.NotFound();
        }

        return await StreamAvatarAsync(user.Id, attachments, cancellationToken);
    }

    private static async Task<IResult> StreamAvatarAsync(
        int userId,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var download = await attachments.OpenReadByOwnerAsync(
            IdentityProfilePolicy.AvatarOwner(userId),
            cancellationToken);
        if (download is null)
        {
            throw ApiProblemException.NotFound();
        }

        return Results.Stream(download.Content, download.Descriptor.ContentType, enableRangeProcessing: false);
    }

    private static async Task<IResult> DeleteAvatarAsync(
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, userManager);
        return await attachments.DeleteByOwnerAsync(
            IdentityProfilePolicy.AvatarOwner(user.Id),
            cancellationToken)
            ? TypedResults.NoContent()
            : throw ApiProblemException.NotFound();
    }

    private static async Task<SegarisUser> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<SegarisUser> userManager) =>
        await userManager.GetUserAsync(principal) ?? throw ApiProblemException.NotFound();

    private static ProfileResponse ToResponse(SegarisUser user, bool hasAvatar) => new(
        user.DisplayName,
        user.Language,
        hasAvatar ? IdentityProfilePolicy.AvatarUrl(user.Id) : null);

    internal sealed record UpdateProfileRequest(string? DisplayName, string? Language);

    internal sealed record ProfileResponse(string DisplayName, string Language, string? AvatarUrl);

    internal sealed record AvatarResponse(string AvatarUrl, string ContentType, long Size);
}
