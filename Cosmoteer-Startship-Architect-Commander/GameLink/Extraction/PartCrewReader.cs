using System.Text.RegularExpressions;
using Cosmoteer.Data;
using Halfling.ObjectText;

namespace Cosmoteer.GameLink.Extraction;

/// <summary>
///     Reads the <c>PartCrew</c> components (any component with <c>Type = PartCrew</c>) of a registered part (see <see cref="PartRegistry" />) using the game's own ObjectText parser.
///     Unlike the regex approach, this sees through inheritance (<c>Part : &lt;../base_part.rules&gt;/Part</c>) and references,
///     so a PartCrew that is (partially) defined in a base file is still found.
/// </summary>
public static class PartCrewReader
{
    // The override templates remove and replace <file>/Part/Components/<name> for every PartCrew component
    private const string ComponentsPath = "Components";

    // Written by the override templates themselves, everything else is copied from the original component
    private static readonly HashSet<string> GeneratedFields = ["Type", "Crew", "PrerequisitesBeforeCrewing", "OverridePriorityKey"];

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

        List<string> problems = [];

        // Most parts have a single PartCrew, but e.g. the manipulator beam has PartCrew1..4 with one crew per beam
        // Components without crew (e.g. crewless variants of the manipulator beam in other mods) don't need an override, so they are left as is
        var crewComponents = components.Names.Distinct()
            .Select(x => FindCrewComponent(components, x, problems))
            .OfType<OTGroupNode>()
            .Where(x => !HasNoCrew(x))
            .ToList();
        if (crewComponents.Count == 0)
            return problems.Count == 0 ? PartExtractionResult.NotACrewedPart(file) : PartExtractionResult.Failed(file, problems.ToArray());

        List<(OTParentNode owner, CrewData data)> crewData = [];

        foreach (var component in crewComponents)
        {
            // Only prefix the problems when there's more than one component, so single crew parts read as before
            var componentProblems = new List<string>();
            if (ReadComponent(component, componentProblems) is { } data) crewData.Add((component.Parent!, data));
            problems.AddRange(crewComponents.Count == 1 ? componentProblems : componentProblems.Select(x => $"{component.Name}: {x}"));
        }

        if (crewData.Count != crewComponents.Count)
            return PartExtractionResult.Failed(file, problems.ToArray());

        // Override the components in the group that defines them, inheriting parts get the override through their base
        var targets = crewData
            .GroupBy(x => x.owner, x => x.data)
            .Select(x => new CrewOverrideTarget(GetFilePath(x.Key), x.Key.PathInFile.Trim('/'), x.ToList()))
            .ToList();

        // The values referring to the removed crew can be in the defining file (inherited by the part) and in the part itself
        var crewCounts = crewData.ToDictionary(x => x.data.ComponentName, x => x.data.CrewCount);
        var removed = crewComponents.Select(x => x.PathWithFile).ToHashSet();
        var replacements = new List<CrewReferenceReplacement>();
        var valid = true;

        foreach (var root in crewData.Select(x => x.owner.Parent).Append(part).OfType<OTParentNode>().DistinctBy(x => x.PathWithFile))
            valid &= FindCrewReferences(root, crewCounts, removed, replacements, problems);

        if (!valid)
            return PartExtractionResult.Failed(file, problems.ToArray());

        return new PartExtractionResult(file, PartExtractionStatus.Extracted, new PartCrewData(targets, replacements), problems);
    }

    /// <summary>
    ///     The component as a PartCrew, or null if it's something else.
    ///     Components that can't be resolved (e.g. inheriting from a root node another mod adds, like <c>&amp;/SW_SHOTS/...</c>) are skipped,
    ///     unless they're written as a PartCrew, then the crew can't be read.
    /// </summary>
    private static OTGroupNode? FindCrewComponent(OTGroupNode components, string name, List<string> problems)
    {
        if (!components.TryGetNode(name, out IOTNode? node)) return null;

        try
        {
            return RulesText.Dereference(node!) is OTGroupNode component && IsPartCrew(component) ? component : null;
        }
        catch (OTNavigateException e)
        {
            if (node is OTParentNode local && local.LocalChildren.Any(x => x is OTFieldNode { Name: "Type", Value: "PartCrew" }))
                problems.Add($"{name}: Unresolvable reference: {e.Message}");
            return null;
        }
    }

    private static string GetFilePath(IOTNode node) => node.FileRoot!.FilePath!.ToString();

    private static bool IsPartCrew(OTGroupNode component) =>
        component.TryGetNode("Type", out IOTNode? typeNode) && RulesText.Dereference(typeNode!) is OTFieldNode { Value: "PartCrew" };

    private static bool HasNoCrew(OTGroupNode component) =>
        component.TryGetNode("Crew", out IOTNode? crewNode) && RulesText.Dereference(crewNode!) is OTFieldNode field && int.TryParse(field.Value, out var crew) && crew == 0;

    private static CrewData? ReadComponent(OTGroupNode partCrew, List<string> problems)
    {
        var crewCount = ReadCrewCount(partCrew, problems);
        var crewingPrerequisites = ReadPrerequisites(partCrew, problems);
        var priorityKey = partCrew.TryGetNode("OverridePriorityKey", out IOTNode? keyNode) && RulesText.Dereference(keyNode!) is OTFieldNode key ? key.Value : null;

        List<string> fields = [];
        var fieldsValid = true;

        foreach (var name in partCrew.Names.Distinct().Where(x => !GeneratedFields.Contains(x)))
        {
            if (partCrew.TryGetNode(name, out IOTNode? node) && RulesText.ToPortableText(node!, problems) is { } value)
                fields.Add($"{name} = {value}");
            else
                fieldsValid = false;
        }

        if (crewCount is not > 0 || crewingPrerequisites == null || !fieldsValid)
            return null;

        return new CrewData
        (
            componentName: partCrew.Name,
            crewCount: crewCount.Value.ToString(),
            crewingPrerequisites: crewingPrerequisites,
            priorityKey: priorityKey,
            fields: fields
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

    private static IReadOnlyList<string>? ReadPrerequisites(OTGroupNode partCrew, List<string> problems)
    {
        // Optional in game, the override still adds its own toggle
        if (!partCrew.TryGetNode("PrerequisitesBeforeCrewing", out IOTNode? node)) return [];

        if (RulesText.Dereference(node!) is not OTParentNode list)
        {
            problems.Add($"PrerequisitesBeforeCrewing is not a list: {RulesText.ToRulesText(node!)}");
            return null;
        }

        var items = list.GetChildren().Select(x => RulesText.ToPortableText(x, problems)).ToList();
        return items.Any(x => x == null) ? null : items.OfType<string>().ToList();
    }

    /// <summary>
    ///     Finds the values written under <paramref name="root" /> that refer to the <c>Crew</c> of a removed component,
    ///     e.g. <c>Stats/CrewRequired = &amp;~/Part/Components/PartCrew/Crew</c>. Those references break once the component is removed, so they are replaced by the crew count.
    /// </summary>
    /// <returns>False if a reference can't be replaced</returns>
    private static bool FindCrewReferences(OTParentNode root, Dictionary<string, string> crewCounts, HashSet<string> removed, List<CrewReferenceReplacement> replacements, List<string> problems)
    {
        var crewReference = new Regex($@"(?:^|/)(?<name>{string.Join('|', crewCounts.Keys.Select(Regex.Escape))})/Crew$");
        var file = GetFilePath(root);
        var valid = true;

        // Only what's written in this file can be replaced in it, inherited values are found through the file they're written in
        foreach (var node in LocalNodes(root).Where(x => !removed.Any(r => x.PathWithFile.StartsWith(r))))
        {
            var path = node.PathInFile.Trim('/');

            if (node is OTReferenceNode reference)
            {
                if (crewReference.Match(reference.Target) is { Success: true } match)
                    replacements.Add(new CrewReferenceReplacement(file, path, crewCounts[match.Groups["name"].Value]));
                continue;
            }

            if (node is OTParentNode) continue;

            var text = RulesText.ToRulesText(node);
            var matched = false;
            var replaced = RulesText.ExpressionReference.Replace(text, x =>
            {
                if (crewReference.Match(x.Groups["target"].Value) is not { Success: true } match) return x.Value;
                matched = true;
                return crewCounts[match.Groups["name"].Value];
            });

            if (!matched) continue;

            // The replacement is written in another file, so the rest of the expression has to work from there
            if (RulesText.ExpressionReference.Matches(replaced).Any(x => !RulesText.IsPortableTarget(x.Groups["target"].Value)))
            {
                problems.Add($"{path} uses the crew in an expression with other references: {text}");
                valid = false;
                continue;
            }

            replacements.Add(new CrewReferenceReplacement(file, path, replaced));
        }

        return valid;
    }

    private static IEnumerable<IOTNode> LocalNodes(OTParentNode parent)
    {
        foreach (var child in parent.LocalChildren)
        {
            yield return child;
            if (child is OTParentNode childParent)
                foreach (var descendant in LocalNodes(childParent))
                    yield return descendant;
        }
    }
}
