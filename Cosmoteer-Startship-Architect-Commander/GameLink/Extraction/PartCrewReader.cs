using Cosmoteer.Data;
using Halfling.ObjectText;

namespace Cosmoteer.GameLink.Extraction;

/// <summary>
///     Reads the <c>Components/PartCrew</c> component of a registered part (see <see cref="PartRegistry" />) using the game's own ObjectText parser.
///     Unlike the regex approach, this sees through inheritance (<c>Part : &lt;../base_part.rules&gt;/Part</c>) and references,
///     so a PartCrew that is (partially) defined in a base file is still found.
/// </summary>
public static class PartCrewReader
{
    // The override templates remove and replace <file>/Part/Components/<name> for every PartCrew component
    private const string ComponentsPath = "Components";

    /// <param name="file">The file path to report, in the form the writers expect</param>
    /// <param name="part">The parsed <c>Part</c> node</param>
    public static PartExtractionResult Read(string file, OTGroupNode part)
    {
        try
        {
            return ReadPartCrew(file, part);
        }
        catch (OTNavigateException e)
        {
            // Usually an inherited base file that doesn't exist, e.g. a dependency on another mod
            return PartExtractionResult.Failed(file, $"Unresolvable reference: {e.Message}");
        }
    }

    private static PartExtractionResult ReadPartCrew(string file, OTGroupNode part)
    {
        if (!part.TryFindAtPath(ComponentsPath, out var componentsNode) || componentsNode is not OTGroupNode components)
            return PartExtractionResult.NotACrewedPart(file);

        // Most parts have a single PartCrew, but e.g. the manipulator beam has PartCrew1..4 with one crew per beam
        // Components without crew (e.g. crewless variants of the manipulator beam in other mods) don't need an override, so they are left as is
        var crewComponents = components.GetChildren().OfType<OTGroupNode>().Where(x => IsPartCrew(x) && !HasNoCrew(x)).ToList();
        if (crewComponents.Count == 0)
            return PartExtractionResult.NotACrewedPart(file);

        List<string> problems = [];
        List<CrewData> crewData = [];

        foreach (var component in crewComponents)
        {
            // Only prefix the problems when there's more than one component, so single crew parts read as before
            var componentProblems = new List<string>();
            if (ReadComponent(component, componentProblems) is { } data) crewData.Add(data);
            problems.AddRange(crewComponents.Count == 1 ? componentProblems : componentProblems.Select(x => $"{component.Name}: {x}"));
        }

        if (crewData.Count != crewComponents.Count)
            return PartExtractionResult.Failed(file, problems.ToArray());

        return new PartExtractionResult(file, PartExtractionStatus.Extracted, crewData, problems);
    }

    private static bool IsPartCrew(OTGroupNode component) =>
        component.TryGetNode("Type", out IOTNode? typeNode) && RulesText.Dereference(typeNode!) is OTFieldNode { Value: "PartCrew" };

    private static bool HasNoCrew(OTGroupNode component) =>
        component.TryGetNode("Crew", out IOTNode? crewNode) && RulesText.Dereference(crewNode!) is OTFieldNode field && int.TryParse(field.Value, out var crew) && crew == 0;

    private static CrewData? ReadComponent(OTGroupNode partCrew, List<string> problems)
    {
        var crewCount = ReadCrewCount(partCrew, problems);
        var defaultPriority = ReadDefaultPriority(partCrew, problems);
        var locations = ReadList(partCrew, "CrewLocations", false, Environment.NewLine, problems);
        var destinations = ReadList(partCrew, "CrewDestinations", true, Environment.NewLine, problems);
        var crewingPrerequisites = ReadList(partCrew, "PrerequisitesBeforeCrewing", false, ", ", problems);
        var highPriorityPrerequisites = ReadList(partCrew, "HighPriorityPrerequisites", false, ", ", null) ?? string.Empty;

        if (crewCount is not > 0 || defaultPriority == null || locations == null || destinations == null || crewingPrerequisites == null)
            return null;

        if (highPriorityPrerequisites.Length == 0) problems.Add("No HighPriorityPrerequisites");

        return new CrewData
        (
            componentName: partCrew.Name,
            locations: locations,
            destinations: destinations,
            crewCount: crewCount.Value.ToString(),
            defaultPriority: defaultPriority,
            crewingPrerequisites: crewingPrerequisites,
            highPriorityPrerequisites: highPriorityPrerequisites
        );
    }

    private static int? ReadCrewCount(OTGroupNode partCrew, List<string> problems)
    {
        if (!partCrew.TryGetNode("Crew", out IOTNode? node))
        {
            problems.Add("Missing Crew");
            return null;
        }

        if (RulesText.Dereference(node) is OTFieldNode field && int.TryParse(field.Value, out var crewCount))
        {
            if (crewCount <= 0) problems.Add($"Crew is {crewCount}");
            return crewCount;
        }

        problems.Add($"Crew is not a number: {RulesText.ToRulesText(node)}");
        return null;
    }

    /// <summary>
    ///     The templates write this as <c>DefaultPriority = &amp;{DefaultPriority}</c>, so we need the reference target, not the value it points at.
    /// </summary>
    private static string? ReadDefaultPriority(OTGroupNode partCrew, List<string> problems)
    {
        if (!partCrew.TryGetNode("DefaultPriority", out IOTNode? node))
        {
            problems.Add("Missing DefaultPriority");
            return null;
        }

        if (node is OTReferenceNode reference)
        {
            // A relative reference would point somewhere else once it's copied into the override file
            if (!RulesText.IsGameRootReference(reference) && !reference.Target.StartsWith('<'))
                problems.Add($"DefaultPriority is a relative reference: {reference.Target}");
            return reference.Target;
        }

        problems.Add($"DefaultPriority is not a reference: {RulesText.ToRulesText(node)}");
        return null;
    }

    /// <param name="problems">Where to report a missing list, or null if the list is optional</param>
    private static string? ReadList(OTGroupNode partCrew, string name, bool dereference, string separator, List<string>? problems)
    {
        if (!partCrew.TryGetNode(name, out IOTNode? node))
        {
            problems?.Add($"Missing {name}");
            return null;
        }

        if (node is OTParentNode list) return RulesText.ListItems(list, dereference, separator);

        problems?.Add($"{name} is not a list: {RulesText.ToRulesText(node)}");
        return null;
    }
}
