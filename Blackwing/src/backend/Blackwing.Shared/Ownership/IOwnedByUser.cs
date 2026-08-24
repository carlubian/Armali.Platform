namespace Blackwing.Shared.Ownership;

/// <summary>
/// Every piece of user content in Blackwing belongs to exactly one account and is
/// never shared, not even with an administrator. Implementing this interface opts an
/// entity into the global ownership filter and into owner stamping on insert; see
/// <c>BlackwingDbContext</c>.
/// </summary>
public interface IOwnedByUser
{
    int OwnerUserId { get; set; }
}
