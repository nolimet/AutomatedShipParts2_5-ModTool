using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Cosmoteer.GameLink;

/// <summary>
///     Resolves the game's managed and native assemblies from its Bin folder, since they are linked with Private=false and never copied to the output.
///     Must be registered before any method that touches a game type is JIT-compiled.
/// </summary>
public static class GameAssemblyResolver
{
    public static bool IsLinkedAtBuild =>
#if COSMOTEER_LINKED
        true;
#else
        false;
#endif

    public static string? BuildTimeBinPath => typeof(GameAssemblyResolver).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(x => x.Key == "CosmoteerBinPath")?.Value;

    public static string? BinPath { get; private set; }

    public static bool Register(string? fallbackGamePath)
    {
        if (BinPath != null) return true;

        string?[] candidates = [BuildTimeBinPath, string.IsNullOrWhiteSpace(fallbackGamePath) ? null : Path.Combine(fallbackGamePath, "Bin")];
        BinPath = candidates.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && File.Exists(Path.Combine(x, "Cosmoteer.dll")));
        if (BinPath == null) return false;

        AssemblyLoadContext.Default.Resolving += OnResolving;
        AssemblyLoadContext.Default.ResolvingUnmanagedDll += OnResolvingUnmanagedDll;
        return true;
    }

    private static Assembly? OnResolving(AssemblyLoadContext context, AssemblyName name)
    {
        var candidate = Path.Combine(BinPath!, $"{name.Name}.dll");
        return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
    }

    private static IntPtr OnResolvingUnmanagedDll(Assembly assembly, string name)
    {
        var fileName = name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.dll";
        var candidate = Path.Combine(BinPath!, fileName);
        return File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle) ? handle : IntPtr.Zero;
    }
}
