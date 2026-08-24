using System.Reflection;

namespace Blackwing.Shared;

/// <summary>
/// Assembly marker for <c>Blackwing.Shared</c>. Architecture tests use it to inspect the
/// dependency direction without taking a reference on an arbitrary implementation type.
/// </summary>
public static class SharedAssembly
{
    public static Assembly Assembly => typeof(SharedAssembly).Assembly;
}
