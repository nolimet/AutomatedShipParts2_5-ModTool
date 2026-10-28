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
    public static (PartCrewData crew, int partCount, Grid report, uint issueCount) GetVanillaParts(string gamePath) =>
        ToReport(ExtractVanilla(gamePath));

    /// <param name="ignoredParts">Regexes matched against the part's file path, matching parts are skipped</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static (PartCrewData crew, int partCount, Grid report, uint issueCount) GetModParts(string modPath, string gamePath, IReadOnlyList<string>? ignoredParts) =>
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
            if (result.Status != PartExtractionStatus.NotACrewedPart) results.Add(ToRootPaths(rootPath, result));
        }

        return results;
    }

    /// <summary>
    ///     Puts the files of the targets and replacements in the same form as the part files, which is what the writers use to build the paths.
    ///     Crew inherited from outside the root (e.g. a mod part based on a vanilla part) is overridden by the writer of that root, so it's left out here.
    /// </summary>
    private static PartExtractionResult ToRootPaths(string rootPath, PartExtractionResult result)
    {
        if (result.CrewData is not { } crew) return result;

        List<string> problems = [..result.Problems];

        List<CrewOverrideTarget> targets = [];
        foreach (var target in crew.Targets)
        {
            if (ToRootPath(rootPath, target.File) is { } file)
                targets.Add(new CrewOverrideTarget(file, target.ComponentsPath, target.CrewComponents));
            else
                problems.Add($"Crew is inherited from {target.File}, it's overridden there");
        }

        var replacements = crew.Replacements
            .Select(x => ToRootPath(rootPath, x.File) is { } file ? new CrewReferenceReplacement(file, x.Path, x.Value) : (CrewReferenceReplacement?)null)
            .OfType<CrewReferenceReplacement>()
            .ToList();

        return result with { CrewData = new PartCrewData(targets, replacements), Problems = problems };
    }

    private static string? ToRootPath(string rootPath, string file)
    {
        var relativePath = Path.GetRelativePath(rootPath, PartRegistry.GetPathAsOnDisk(Path.GetFullPath(file)));
        return relativePath.StartsWith("..") || Path.IsPathRooted(relativePath) ? null : Path.Combine(rootPath, relativePath);
    }

    private static (PartCrewData crew, int partCount, Grid report, uint issueCount) ToReport(IReadOnlyList<PartExtractionResult> results)
    {
        var extracted = results.Where(x => x.Status == PartExtractionStatus.Extracted).Select(x => x.CrewData!.Value).ToList();

        // Parts that inherit the same crew (e.g. mk2 and mk3 from mk1) share one override
        var crew = new PartCrewData(
            extracted.SelectMany(x => x.Targets).DistinctBy(x => (x.File, x.ComponentsPath)).ToList(),
            extracted.SelectMany(x => x.Replacements).DistinctBy(x => (x.File, x.Path)).ToList()
        );

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

        return (crew, extracted.Count, grid, (uint)issues.Length);
    }
}
