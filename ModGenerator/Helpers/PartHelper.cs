using System;
using System.Text.RegularExpressions;
using ModGenerator.Data;
using Spectre.Console;

namespace ModGenerator.Helpers;

public static class PartHelper
{
    // Remove end-of-line comments like // ... or /// ...
    private static readonly Regex LineCommentRegex = new(@"(?m)//.*$");

    // Balanced [ ... ] block after "CrewDestinations" on the same line
    private static readonly Regex DestinationsRegex = new(
        @"(?m)^\s*CrewDestinations\b[^\[]*\[(?<content>(?>[^\[\]]+|\[(?<d>)|\](?<-d>))*)(?(d)(?!))\]",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    // Balanced [ ... ] block after "CrewLocations" on the same line
    private static readonly Regex LocationsRegex = new(
        @"(?m)^\s*CrewLocations\b[^\[]*\[(?<content>(?>[^\[\]]+|\[(?<d>)|\](?<-d>))*)(?(d)(?!))\]",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    // Balanced { ... } block for the PartCrew component
    private static readonly Regex PartCrewBlockRegex = new(
        @"(?ms)^\s*PartCrew\b[^{]*\{(?<content>(?>[^{}]+|\{(?<d>)|\}(?<-d>))*)(?(d)(?!))\}",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    private static readonly Regex CrewCountRegex = new(@"Crew ?= ?(\d+)");
    // Capture the value on the right side (without the leading '&' if present)
    private static readonly Regex DefaultPriorityRegex = new(@"(?m)^\s*DefaultPriority\s*=\s*&?(\S+)");
    private static readonly Regex CrewingRequirementsRegex = new(@"PrerequisitesBeforeCrewing = \[([\S ]+?)\]");
    private static readonly Regex HighPriorityPrerequisitesRegex = new(@"HighPriorityPrerequisites = \[(\S+?)\]");

    // Allow names like "CrewLocation" (no suffix) or "CrewLocation4", etc.
    private static readonly Regex CrewLocationBlockRegex = new(
        @"(?ms)^\s*(?<name>CrewLocation[^\s:{]*)\s*(?::[^\{]+)?\{(?<content>(?>[^{}]+|\{(?<d>)|\}(?<-d>))*)(?(d)(?!))\}",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    // Extract the literal Location = [x, y] line inside a CrewLocation block
    private static readonly Regex LocationValueRegex = new(@"(?m)^\s*Location\s*=\s*(\[[^\]]+\])");

    // Accept &../..., &../../..., etc. and capture the terminal name (e.g., CrewLocation or CrewLocation4)
    private static readonly Regex CrewDestinationRefRegex = new(
        @"^\s*&(?:\.\./)+(?:)(?<name>[A-Za-z0-9_]+)/Location\s*$",
        RegexOptions.Compiled);

    public static (Dictionary<string, CrewData> parts, Grid report) GetParts(string path, IReadOnlyList<string>? ignoredParts = null)
    {
        Dictionary<string, CrewData> loadedRules = new();
        Grid issuesGrid = new();
        issuesGrid.AddColumns(7);
        issuesGrid.AddRow("Part", "Locations", "Destinations", "CrewCount", "DefaultPriority", "CrewingRequirements", "HighPriorityPrerequisites");

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

        // 2) Run robust, anchored regexes with balanced-bracket core
        var locationsResults = LocationsRegex.Match(data);
        var destinationsResults = DestinationsRegex.Match(data);
        var crewCountResult = CrewCountRegex.Match(data);

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
            if (locValue.Success)
            {
                crewLocationCoords[name] = locValue.Groups[1].Value.Trim();
            }
        }

        // Resolve CrewDestinations: replace &../../CrewLocationX/Location with the literal coordinates if available
        string resolvedDestinations = string.Empty;
        if (destinationsResults.Success)
        {
            var destBlock = destinationsResults.Groups["content"].Value;
            var lines = destBlock.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var resolved = new List<string>(lines.Length);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                    continue;

                var refMatch = CrewDestinationRefRegex.Match(line);
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
                resolved.Add(line);
            }

            resolvedDestinations = string.Join(Environment.NewLine, resolved);
        }

        if (locationsResults.Success && destinationsResults.Success && crewCountResult.Success && defaultPriorityResult.Success && crewingRequirementsResult.Success && highPriorityPrerequisitesResult.Success)
        {
            loadedRules[file] = new CrewData
            (
                locations: locationsResults.Groups["content"].Value.Trim(),
                destinations: resolvedDestinations.Trim(),
                crewCount: crewCountResult.Groups[1].Value,
                defaultPriority: defaultPriorityResult.Groups[1].Value,
                crewingPrerequisites: crewingRequirementsResult.Groups[1].Value,
                highPriorityPrerequisites: highPriorityPrerequisitesResult.Groups[1].Value
            );
        }
        else if (crewCountResult.Success)
        {
            issuesGrid.AddRow(
                Path.GetFileNameWithoutExtension(file),
                locationsResults.Success.ToString(),
                destinationsResults.Success.ToString(),
                crewCountResult.Groups[1].Value,
                defaultPriorityResult.Success.ToString(),
                crewingRequirementsResult.Success.ToString(),
                highPriorityPrerequisitesResult.Success.ToString()
            );
        }
    }

    private static string StripLineComments(string s) => LineCommentRegex.Replace(s, "");
}