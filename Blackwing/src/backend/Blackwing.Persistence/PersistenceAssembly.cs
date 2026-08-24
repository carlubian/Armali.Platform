using System.Reflection;

namespace Blackwing.Persistence;

/// <summary>
/// Assembly marker for <c>Blackwing.Persistence</c>. Architecture tests use it to inspect the
/// dependency direction without taking a reference on an arbitrary implementation type.
/// </summary>
public static class PersistenceAssembly
{
    public static Assembly Assembly => typeof(PersistenceAssembly).Assembly;
}
