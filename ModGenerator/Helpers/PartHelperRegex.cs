using System.Text.RegularExpressions;

namespace ModGenerator.Helpers;

public static partial class PartHelperRegex
{
    // Remove end-of-line comments like // ... or /// ...
    [GeneratedRegex(@"(?m)//.*$", RegexOptions.Compiled, 2000)]
    public static partial Regex LineCommentRegex();

    // Balanced [ ... ] block after "CrewDestinations" on the same line
    [GeneratedRegex(@"(?m)^\s*CrewDestinations\b[^\[]*\[(?<content>(?>[^\[\]]+|\[(?<d>)|\](?<-d>))*)(?(d)(?!))\]", RegexOptions.Compiled, 2000)]
    public static partial Regex DestinationsRegex();

    // Balanced [ ... ] block after "CrewLocations" on the same line
    [GeneratedRegex(@"(?m)^\s*CrewLocations\b[^\[]*\[(?<content>(?>[^\[\]]+|\[(?<d>)|\](?<-d>))*)(?(d)(?!))\]", RegexOptions.Compiled, 2000)]
    public static partial Regex LocationsRegex();

    // Balanced { ... } block for the PartCrew component
    [GeneratedRegex(@"(?ms)^\s*PartCrew\b[^{]*\{(?<content>(?>[^{}]+|\{(?<d>)|\}(?<-d>))*)(?(d)(?!))\}", RegexOptions.Compiled, 2000)]
    public static partial Regex PartCrewBlockRegex();

    [GeneratedRegex(@"Crew ?= ?(\d+)", RegexOptions.Compiled, 2000)]
    public static partial Regex CrewCountRegex();

    // Capture the value on the right side (without the leading '&' if present)
    [GeneratedRegex(@"(?m)^\s*DefaultPriority\s*=\s*&?(\S+)", RegexOptions.Compiled, 2000)]
    public static partial Regex DefaultPriorityRegex();

    [GeneratedRegex(@"PrerequisitesBeforeCrewing = \[([\S ]+?)\]", RegexOptions.Compiled, 2000)]
    public static partial Regex CrewingRequirementsRegex();

    [GeneratedRegex(@"HighPriorityPrerequisites = \[(\S+?)\]", RegexOptions.Compiled, 2000)]
    public static partial Regex HighPriorityPrerequisitesRegex();

    // Allow names like "CrewLocation" (no suffix) or "CrewLocation4", etc.
    [GeneratedRegex(@"(?ms)^\s*(?<name>CrewLocation[^\s:{]*)\s*(?::[^\{]+)?\{(?<content>(?>[^{}]+|\{(?<d>)|\}(?<-d>))*)(?(d)(?!))\}", RegexOptions.Compiled, 2000)]
    public static partial Regex CrewLocationBlockRegex();

    // Extract the literal Location = [x, y] line inside a CrewLocation block
    [GeneratedRegex(@"(?m)^\s*Location\s*=\s*(\[[^\]]+\])", RegexOptions.Compiled, 2000)]
    public static partial Regex LocationValueRegex();

    // Accept &../..., &../../..., etc. and capture the terminal name (e.g., CrewLocation or CrewLocation4)
    [GeneratedRegex(@"^\s*&(?:\.\./)+(?:)(?<name>[A-Za-z0-9_]+)/Location\s*$", RegexOptions.Compiled, 2000)]
    public static partial Regex CrewDestinationRefRegex();
}
