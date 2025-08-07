using System.Text.RegularExpressions;
using ModGenerator.Data;

namespace ModGenerator.Helpers;

public static class PartHelper
{
    private static readonly Regex DestinationsRegex = new(@"^\s*CrewDestinations\s*\[\s*([\s\S]*?)\s*\]", RegexOptions.Multiline);
    private static readonly Regex LocationsRegex = new(@"^\s*CrewLocations\s*\[\s*([\s\S]*?)\s*\]", RegexOptions.Multiline);
    private static readonly Regex CrewCountRegex = new(@"Crew = (.+?)");
    private static readonly Regex DefaultPriorityRegex = new(@"DefaultPriority = &/PRIORITIES/(\S*)");
    private static readonly Regex CrewingRequirementsRegex = new(@"PrerequisitesBeforeCrewing = \[(\S+)\]");
    private static readonly Regex HighPriorityPrerequisitesRegex = new(@"HighPriorityPrerequisites = \[(\S+)\]");

    public static Dictionary<string, CrewData> GetParts(string path)
    {
        Dictionary<string, CrewData> loadedRules = new();
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
        }

        return loadedRules;
    }
}
