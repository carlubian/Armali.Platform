using System.Reflection;

namespace Segaris.Persistence;

public static class PersistenceAssembly
{
    public static Assembly Assembly => typeof(PersistenceAssembly).Assembly;
}
