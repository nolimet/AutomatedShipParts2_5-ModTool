using System.Text.RegularExpressions;
using ModGenerator.Data;
using Spectre.Console;

namespace ModGenerator.Helpers;

public static class PartHelper
{
    private static readonly Regex DestinationsRegex = new(@"CrewDestinations.*\s*\[\s*((?:\s*\[.*?\]\s*)+|([\s\S]*?))\]");
    private static readonly Regex LocationsRegex = new(@"CrewLocations.*\s*\[\s*([\s\S]*?)\s*\]");
    private static readonly Regex CrewCountRegex = new(@"Crew ?= ?(\d+)");
    private static readonly Regex DefaultPriorityRegex = new(@"DefaultPriority = &(\S*)");
    private static readonly Regex CrewingRequirementsRegex = new(@"PrerequisitesBeforeCrewing = \[(.+)\]");
    private static readonly Regex HighPriorityPrerequisitesRegex = new(@"HighPriorityPrerequisites = \[(.+)\]");

    public static (Dictionary<string, CrewData> parts, Grid report) GetParts(string path)
    {
        Dictionary<string, CrewData> loadedRules = new();
        Grid issuesGrid = new();
        issuesGrid.AddColumns(7);
        issuesGrid.AddRow("Part", "Locations", "Destinations", "CrewCount", "DefaultPriority", "CrewingRequirements", "HighPriorityPrerequisites");

        foreach (var file in Directory.GetFiles(path, "*.rules", SearchOption.AllDirectories))
        {
            var data = File.ReadAllText(file);
            var locationsResults = LocationsRegex.Match(data);
            var destinationsResults = DestinationsRegex.Match(data);
            var crewCountResult = CrewCountRegex.Match(data);
            var defaultPriorityResult = DefaultPriorityRegex.Match(data);
            var crewingRequirementsResult = CrewingRequirementsRegex.Match(data);
            var highPriorityPrerequisitesResult = HighPriorityPrerequisitesRegex.Match(data);

            if (locationsResults.Success && destinationsResults.Success && crewCountResult.Success && defaultPriorityResult.Success && crewingRequirementsResult.Success && highPriorityPrerequisitesResult.Success)
            {
                loadedRules[file] = new CrewData
                (
                    locations: locationsResults.Groups[1].Value,
                    destinations: destinationsResults.Groups[1].Value,
                    crewCount: crewCountResult.Groups[1].Value,
                    defaultPriority: defaultPriorityResult.Groups[1].Value,
                    crewingPrerequisites: crewingRequirementsResult.Groups[1].Value,
                    highPriorityPrerequisites: highPriorityPrerequisitesResult.Groups[1].Value
                );
            }
            else if (crewCountResult.Success)
                issuesGrid.AddRow(Path.GetFileNameWithoutExtension(file), locationsResults.Success.ToString(), destinationsResults.Success.ToString(), crewCountResult.Groups[1].Value, defaultPriorityResult.Success.ToString(), crewingRequirementsResult.Success.ToString(), highPriorityPrerequisitesResult.Success.ToString());
        }

        return (loadedRules, issuesGrid);
    }
}
