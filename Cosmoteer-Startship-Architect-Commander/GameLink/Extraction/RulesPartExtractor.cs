using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Cosmoteer.Data;
using Spectre.Console;

namespace Cosmoteer.GameLink.Extraction;

/// <summary>
///     Extracts crew data from the parts the game actually loads (see <see cref="PartRegistry" />) using the game's parser.
///     Requires <see cref="GameAssemblyResolver.Register" /> to have been called first.
/// </summary>
/// <remarks>NoInlining keeps the game types out of the callers, so they can be compiled before the game assemblies are resolvable.</remarks>
public static class RulesPartExtractor
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static (Dictionary<string, CrewData> parts, Grid report, uint issueCount) GetVanillaParts(string gamePath) =>
        ToReport(ExtractVanilla(gamePath));

    /// <param name="ignoredParts">Regexes matched against the part's file path, matching parts are skipped</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static (Dictionary<string, CrewData> parts, Grid report, uint issueCount) GetModParts(string modPath, string gamePath, IReadOnlyList<string>? ignoredParts) =>
        ToReport(ExtractMod(modPath, gamePath, ignoredParts));

    private static IReadOnlyList<PartExtractionResult> ExtractVanilla(string gamePath)
    {
        using var scope = new GameDataScope(gamePath);
        var (parts, problems) = PartRegistry.FindVanillaParts(gamePath);
        return Extract(gamePath, parts, problems, null);
    }

    private static IReadOnlyList<PartExtractionResult> ExtractMod(string modPath, string gamePath, IReadOnlyList<string>? ignoredParts)
    {
        using var scope = new GameDataScope(gamePath);
        var (parts, problems) = PartRegistry.FindModParts(modPath);
        return Extract(modPath, parts, problems, ignoredParts);
    }

    private static List<PartExtractionResult> Extract(string rootPath, List<RegisteredPart> parts, List<string> registryProblems, IReadOnlyList<string>? ignoredParts)
    {
        var ignoreRegexes = ignoredParts?.Select(x => new Regex(x, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500))).ToArray() ?? [];
        List<PartExtractionResult> results = [..registryProblems.Select(x => PartExtractionResult.Failed(Path.Combine(rootPath, "mod.rules"), x))];

        foreach (var part in parts)
        {
            var relativePath = Path.GetRelativePath(rootPath, part.File);
            if (relativePath.StartsWith("..") || Path.IsPathRooted(relativePath))
            {
                // e.g. a mod registering a vanilla part again, the action templates can only point inside the mod
                results.Add(PartExtractionResult.Failed(part.File, $"Registered part is outside {rootPath}"));
                continue;
            }

            // Same form as Directory.GetFiles(rootPath) gives, which is what the writers use to build the part paths
            var file = Path.Combine(rootPath, relativePath);

            if (ignoreRegexes.Any(x => x.IsMatch(file)))
            {
                Console.WriteLine($"Skipped: {file}");
                continue;
            }

            var result = PartCrewReader.Read(file, part.Part);
            if (result.Status != PartExtractionStatus.NotACrewedPart) results.Add(result);
        }

        return results;
    }

    private static (Dictionary<string, CrewData> parts, Grid report, uint issueCount) ToReport(IReadOnlyList<PartExtractionResult> results)
    {
        var parts = results
            .Where(x => x.Status == PartExtractionStatus.Extracted)
            .ToDictionary(x => x.File, x => x.CrewData!.Value);

        var issues = results.Where(x => x.Problems.Count > 0).ToArray();

        Grid grid = new();
        grid.AddColumns(3);
        grid.AddRow("Part", "Extracted", "Problems");

        foreach (var issue in issues)
            grid.AddRow(
                Markup.Escape(Path.GetFileNameWithoutExtension(issue.File)),
                issue.Status == PartExtractionStatus.Extracted ? "[green]true[/]" : "[red]false[/]",
                Markup.Escape(string.Join("; ", issue.Problems))
            );

        return (parts, grid, (uint)issues.Length);
    }
}
