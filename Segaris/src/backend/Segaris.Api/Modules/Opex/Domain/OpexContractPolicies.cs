using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Opex.Domain;

/// <summary>
/// Visibility rules for Opex contracts. A contract is accessible when it is
/// public or created by the requesting user; occurrences inherit access through
/// their parent contract and never expose a private record to anyone else.
/// </summary>
internal static class OpexContractPolicies
{
    public static Expression<Func<OpexContract, bool>> AccessibleTo(UserId userId) =>
        contract => contract.Visibility == RecordVisibility.Public || contract.CreatedBy == userId.Value;

    public static Expression<Func<OpexContract, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(OpexContract contract, UserId userId) =>
        contract.CreatedBy == userId.Value;
}
