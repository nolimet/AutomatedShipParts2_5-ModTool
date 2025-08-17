using System.Text.RegularExpressions;
using ModGenerator.Data;
using Spectre.Console;

namespace ModGenerator.Helpers;

public static class PartHelper
{
    private static readonly Regex LineCommentRegex = PartHelperRegex.LineCommentRegex();

    private static readonly Regex DestinationsRegex = PartHelperRegex.DestinationsRegex();

    private static readonly Regex LocationsRegex = PartHelperRegex.LocationsRegex();

    private static readonly Regex PartCrewBlockRegex = PartHelperRegex.PartCrewBlockRegex();

    private static readonly Regex CrewCountRegex = PartHelperRegex.CrewCountRegex();

    private static readonly Regex DefaultPriorityRegex = PartHelperRegex.DefaultPriorityRegex();
    private static readonly Regex CrewingRequirementsRegex = PartHelperRegex.CrewingRequirementsRegex();
    private static readonly Regex HighPriorityPrerequisitesRegex = PartHelperRegex.HighPriorityPrerequisitesRegex();

    private static readonly Regex CrewLocationBlockRegex = PartHelperRegex.CrewLocationBlockRegex();

    private static readonly Regex LocationValueRegex = PartHelperRegex.LocationValueRegex();

    private static readonly Regex CrewDestinationRefRegex = PartHelperRegex.CrewDestinationRefRegex();

    public static (Dictionary<string, CrewData> parts, Grid report) GetParts(string path, IReadOnlyList<string>? ignoredParts = null)
    {
        Dictionary<string, CrewData> loadedRules = new();
        Grid issuesGrid = new();
        issuesGrid.AddColumns(8);
        issuesGrid.AddRow("Part", "Locations", "Destinations", "CrewCount", "DefaultPriority", "CrewingRequirements", "HighPriorityPrerequisites", "Extracted");

        foreach (var file in Directory.GetFiles(path, "*.rules", SearchOption.AllDirectories))
            try
            {
                ExtractPartData(file, ignoredParts, loadedRules, issuesGrid);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        return (loadedRules, issuesGrid);
    }

    private static void ExtractPartData(string file, IReadOnlyList<string>? ignoredParts, Dictionary<string, CrewData> loadedRules, Grid issuesGrid)
    {
        if (ignoredParts is not null)
        {
            if (ignoredParts.Any(x => file.EndsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.WriteLine($"Skipped: {file}");
                return;
            }
        }

        // 1) Read and strip inline comments first to avoid false bracket hits inside comments
        var raw = File.ReadAllText(file);
        var data = StripLineComments(raw);

        var crewCountResult = CrewCountRegex.Match(data);
        if (!crewCountResult.Success) return;

        // 2) Run robust, anchored regexes with balanced-bracket core
        var locationsResults = LocationsRegex.Match(data);
        var destinationsResults = DestinationsRegex.Match(data);

        // Scope DefaultPriority search strictly to the PartCrew block to avoid other DefaultPriority fields (e.g., consumers)
        var partCrewBlock = PartCrewBlockRegex.Match(data);
        var partCrewContent = partCrewBlock.Success ? partCrewBlock.Groups["content"].Value : string.Empty;
        var defaultPriorityResult = partCrewBlock.Success ? DefaultPriorityRegex.Match(partCrewContent) : Match.Empty;

        var crewingRequirementsResult = CrewingRequirementsRegex.Match(data);
        var highPriorityPrerequisitesResult = HighPriorityPrerequisitesRegex.Match(data);

        // Build a map of CrewLocation name -> literal [x, y] Location string
        var crewLocationCoords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match locBlock in CrewLocationBlockRegex.Matches(data))
        {
            var name = locBlock.Groups["name"].Value.Trim();
            var content = locBlock.Groups["content"].Value;
            var locValue = LocationValueRegex.Match(content);
            if (locValue.Success) crewLocationCoords[name] = locValue.Groups[1].Value.Trim();
        }

        // Resolve CrewDestinations: replace &../../CrewLocationX/Location with the literal coordinates if available
        var resolvedDestinations = string.Empty;
        if (destinationsResults.Success)
        {
            var destBlock = destinationsResults.Groups["content"].Value;

            // Split by commas and/or newlines to support both multi-line and single-line lists
            var tokens = destBlock
                .ReplaceLineEndings("\n")
                .Split([',', '\n'], StringSplitOptions.RemoveEmptyEntries);

            var resolved = new List<string>(tokens.Length);
            foreach (var rawToken in tokens)
            {
                var token = rawToken.Trim();
                if (token.Length == 0)
                    continue;

                var refMatch = CrewDestinationRefRegex.Match(token);
                if (refMatch.Success)
                {
                    var refName = refMatch.Groups["name"].Value;
                    if (crewLocationCoords.TryGetValue(refName, out var coords))
                    {
                        // Use the literal coordinates from the matching CrewLocation block
                        resolved.Add(coords);
                        continue;
                    }
                }

                // Fallback: keep the original value as-is
                resolved.Add(token);
            }

            resolvedDestinations = string.Join(Environment.NewLine, resolved);
        }

        var extracted = false;
        if (locationsResults.Success && destinationsResults.Success && crewCountResult.Success && defaultPriorityResult.Success && crewingRequirementsResult.Success)
        {
            loadedRules[file] = new CrewData
            (
                locations: locationsResults.Groups["content"].Value.Trim(),
                destinations: resolvedDestinations.Trim(),
                crewCount: crewCountResult.Groups[1].Value,
                defaultPriority: defaultPriorityResult.Groups[1].Value,
                crewingPrerequisites: crewingRequirementsResult.Groups[1].Value,
                highPriorityPrerequisites: highPriorityPrerequisitesResult.Success ? highPriorityPrerequisitesResult.Groups[1].Value : string.Empty
            );

            extracted = true;
        }

        if (!(locationsResults.Success && destinationsResults.Success && crewCountResult.Success && defaultPriorityResult.Success && crewingRequirementsResult.Success && highPriorityPrerequisitesResult.Success))
        {
            issuesGrid.AddRow(
                Path.GetFileNameWithoutExtension(file),
                locationsResults.Success.ToString(),
                destinationsResults.Success.ToString(),
                crewCountResult.Groups[1].Value,
                defaultPriorityResult.Success.ToString(),
                crewingRequirementsResult.Success.ToString(),
                highPriorityPrerequisitesResult.Success.ToString(),
                extracted.ToString()
            );
        }
    }

    private static string StripLineComments(string s) => LineCommentRegex.Replace(s, "");
}
