namespace Blackwing.Shared.Content;

/// <summary>
/// The three fixed tag types. The numeric values are part of the contract: they are stored as
/// <see cref="int"/> in the database, so a value must never be renumbered or reused.
/// </summary>
public enum TagKind
{
    Person = 0,
    Place = 1,
    Topic = 2,
}
