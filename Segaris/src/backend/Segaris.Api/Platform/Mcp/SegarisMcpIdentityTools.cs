using System.ComponentModel;
using Microsoft.AspNetCore.Identity;
using ModelContextProtocol.Server;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Platform.Mcp;

[McpServerToolType]
internal sealed class SegarisMcpIdentityTools(
    ICurrentUser currentUser,
    UserManager<SegarisUser> userManager)
{
    [McpServerTool]
    [Description("Returns the Segaris user authenticated by the current API key.")]
    public async Task<CurrentUserResponse> identity_get_current_user()
    {
        if (currentUser.UserId is not { } userId)
        {
            throw new ApiProblemException(
                StatusCodes.Status401Unauthorized,
                ApiErrorCodes.Unauthorized,
                "Authentication is required.");
        }

        var user = await userManager.FindByIdAsync(
            userId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (user is null)
        {
            throw ApiProblemException.NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserResponse(
            user.Id,
            user.UserName ?? string.Empty,
            user.DisplayName,
            user.Language,
            roles.Order(StringComparer.Ordinal).ToArray());
    }

    internal sealed record CurrentUserResponse(
        int UserId,
        string UserName,
        string DisplayName,
        string Language,
        IReadOnlyList<string> Roles);
}
