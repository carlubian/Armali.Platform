using System.Globalization;
using Segaris.Api.Modules.Firebird.Contracts;

namespace Segaris.Api.Modules.Firebird.Queries;

internal static class FirebirdAvatarResponseFactory
{
    public static PersonAvatarResponse Placeholder() => new(null, null, "placeholder");

    public static PersonAvatarResponse Avatar(int personId, int attachmentId) => new(
        attachmentId.ToString(CultureInfo.InvariantCulture),
        string.Create(CultureInfo.InvariantCulture, $"/api/people/{personId}/avatar"),
        "avatar");
}
