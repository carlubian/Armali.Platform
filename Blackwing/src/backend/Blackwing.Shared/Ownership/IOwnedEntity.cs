namespace Blackwing.Shared.Ownership;

/// <summary>Contract required for all private gallery aggregates from phase 3 onward.</summary>
public interface IOwnedEntity
{
    Guid OwnerUserId { get; }
}
