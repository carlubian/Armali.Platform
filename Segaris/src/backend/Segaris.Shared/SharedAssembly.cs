using System.Reflection;

namespace Segaris.Shared;

public static class SharedAssembly
{
    public static Assembly Assembly => typeof(SharedAssembly).Assembly;
}

