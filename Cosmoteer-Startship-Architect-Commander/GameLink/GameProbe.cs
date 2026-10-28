using System.Reflection;
using System.Runtime.Versioning;
using Spectre.Console;

namespace Cosmoteer.GameLink;

/// <summary>
///     Validates the game link: loads the game assemblies through <see cref="GameAssemblyResolver" /> and lists public types
///     that look related to rules parsing, to find the API for reading other mods' .rules files.
/// </summary>
public static class GameProbe
{
    private static readonly string[] AssemblyNames = ["Cosmoteer", "HalflingCore"];
    private static readonly string[] DefaultKeywords = ["Rule", "Parse", "Deserial", "Node"];

    public static void Run(string? gamePath, IReadOnlyList<string> keywords)
    {
        if (keywords.Count == 0) keywords = DefaultKeywords;

        AnsiConsole.MarkupLine($"Linked at build: {(GameAssemblyResolver.IsLinkedAtBuild ? "[green]yes[/]" : "[red]no[/]")}");
        AnsiConsole.MarkupLine($"Build-time Bin path: [grey]{Markup.Escape(GameAssemblyResolver.BuildTimeBinPath ?? "none")}[/]");

        if (!GameAssemblyResolver.Register(gamePath))
        {
            AnsiConsole.MarkupLine("[red]Cosmoteer.dll not found at the build-time path or in the configured BaseGamePath.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"Resolving game assemblies from: [green]{Markup.Escape(GameAssemblyResolver.BinPath!)}[/]");
        AnsiConsole.MarkupLine($"Tool runtime: [grey]{Markup.Escape(Environment.Version.ToString())}[/]");

        foreach (var assemblyName in AssemblyNames)
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]Failed to load {assemblyName}[/]");
                AnsiConsole.WriteException(e);
                continue;
            }

            var tree = new Tree($"[bold]{Markup.Escape(assembly.GetName().Name ?? assemblyName)}[/] {assembly.GetName().Version} " +
                                $"[grey]({Markup.Escape(assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "unknown framework")})[/]");
            tree.AddNode($"[grey]{Markup.Escape(assembly.Location)}[/]");

            foreach (var group in GetLoadableTypes(assembly)
                         .Where(x => x.IsPublic && keywords.Any(k => x.Name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                         .GroupBy(x => x.Namespace ?? "<global>")
                         .OrderBy(x => x.Key))
            {
                var namespaceNode = tree.AddNode($"[yellow]{Markup.Escape(group.Key)}[/]");
                foreach (var type in group.OrderBy(x => x.Name))
                    namespaceNode.AddNode(Markup.Escape(type.Name));
            }

            AnsiConsole.Write(tree);
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            AnsiConsole.MarkupLine($"[yellow]{e.LoaderExceptions.Length} type(s) in {Markup.Escape(assembly.GetName().Name ?? "?")} failed to load, showing the rest[/]");
            return e.Types.OfType<Type>();
        }
    }
}
