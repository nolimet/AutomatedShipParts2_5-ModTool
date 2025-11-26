using System.Text.RegularExpressions;
using Cosmoteer.Data;
using Spectre.Console;

namespace Cosmoteer.Helpers;

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

    private static readonly Regex DestinationItemRegex = PartHelperRegex.DestinationItemRegex();

    public static (Dictionary<string, CrewData> parts, Grid report, uint issueCount) GetParts(string path, IReadOnlyList<string>? ignoredParts = null)
    {
        Dictionary<string, CrewData> loadedRules = new();
        Grid issuesGrid = new();
        uint issueCount = 0;
        issuesGrid.AddColumns(9);
        issuesGrid.AddRow("Part", "Locations", "Destinations", "CrewCount", "DefaultPriority", "CrewingRequirements", "HighPriorityPrerequisites", "Extracted", "NoCrew");

        var ignoreListRegexs = ignoredParts?.Select(x => new Regex(x, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500))).ToArray() ?? [];

        foreach (var file in Directory.GetFiles(path, "*.rules", SearchOption.AllDirectories))
            try
            {
                ExtractPartData(file, ignoreListRegexs, loadedRules, issuesGrid, ref issueCount);
                ;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        return (loadedRules, issuesGrid, issueCount);
    }

    private static void ExtractPartData(string file, IReadOnlyList<Regex> ignoredParts, Dictionary<string, CrewData> loadedRules, Grid issuesGrid, ref uint issueCount)
    {
        if (ignoredParts.Count > 0)
        {
            if (ignoredParts.Any(x => x.IsMatch(file)))
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

        // Resolve CrewDestinations: parse items as either [x, y] or &../../.../Location
        var resolvedDestinations = string.Empty;
        if (destinationsResults.Success)
        {
            var destBlock = destinationsResults.Groups["content"].Value;

            var matches = DestinationItemRegex.Matches(destBlock);
            var resolved = new List<string>(matches.Count);
            foreach (Match m in matches)
            {
                var token = m.Value.Trim();
                if (token.Length == 0) continue;

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

                // Fallback: keep the original value as-is (covers [x, y] literals)
                resolved.Add(token);
            }

            resolvedDestinations = string.Join(Environment.NewLine, resolved);
        }

        var crewCount = int.TryParse(crewCountResult.Groups[1].Value, out var crewCountResultInt) ? crewCountResultInt : -1;
        var extracted = false;
        if (locationsResults.Success && destinationsResults.Success && crewCountResult.Success && defaultPriorityResult.Success && crewingRequirementsResult.Success && crewCount > 0)
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

        if (locationsResults.Success && destinationsResults.Success && crewCountResult.Success && defaultPriorityResult.Success && crewingRequirementsResult.Success && highPriorityPrerequisitesResult.Success && crewCount > 0) return;

        issueCount++;
        issuesGrid.AddRow(
            Path.GetFileNameWithoutExtension(file),
            locationsResults.Success.ToString(),
            destinationsResults.Success.ToString(),
            crewCountResult.Groups[1].Value,
            defaultPriorityResult.Success.ToString(),
            crewingRequirementsResult.Success.ToString(),
            highPriorityPrerequisitesResult.Success.ToString(),
            extracted.ToString(),
            crewCount > 0 ? "false" : "true"
        );
    }

    private static string StripLineComments(string s) => LineCommentRegex.Replace(s, "");
}
