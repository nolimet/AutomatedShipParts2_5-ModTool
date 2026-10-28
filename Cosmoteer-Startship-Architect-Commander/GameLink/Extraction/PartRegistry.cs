using Halfling.ObjectText;

namespace Cosmoteer.GameLink.Extraction;

/// <summary>A part the game actually loads: the file it's defined in and its parsed <c>Part</c> node.</summary>
public sealed record RegisteredPart(string File, OTGroupNode Part);

/// <summary>
///     Finds the parts the game actually loads, instead of every .rules file in a folder.
///     Mods often ship files that are never loaded (old versions, commented out registrations, base files), those are not parts in game.
/// </summary>
public static class PartRegistry
{
    private const string PartsListName = "Parts";

    /// <summary>
    ///     Vanilla parts are listed per faction, e.g. <c>Data/ships/terran/terran.rules</c> → <c>Terran/Parts</c>.
    /// </summary>
    public static (List<RegisteredPart> parts, List<string> problems) FindVanillaParts(string gamePath)
    {
        List<RegisteredPart> parts = [];
        List<string> problems = [];

        foreach (var factionFolder in Directory.GetDirectories(Path.Combine(gamePath, "Data", "ships")))
        {
            var factionFile = Path.Combine(factionFolder, $"{Path.GetFileName(factionFolder)}.rules");
            if (!File.Exists(factionFile) || Load(factionFile, problems) is not { } faction) continue;

            foreach (var ship in faction.GetChildren().OfType<OTGroupNode>())
                if (ship.TryGetNode(PartsListName, out IOTNode? partsList) && partsList is OTListNode list)
                    foreach (var entry in list.GetChildren())
                        AddPart(entry, parts, problems);
        }

        return (Distinct(parts), problems);
    }

    /// <summary>
    ///     Mod parts are registered by actions in <c>mod.rules</c> that add to a parts list, e.g.
    ///     <c>{ Action = AddMany; AddTo = "&lt;ships/terran/terran.rules&gt;/Terran/Parts"; ManyToAdd = [&amp;&lt;my_part/my_part.rules&gt;/Part] }</c>.
    ///     Actions can also come from other files, e.g. <c>&amp;&lt;Modifications/Changes.rules&gt;/Actions</c> inside the Actions list.
    /// </summary>
    public static (List<RegisteredPart> parts, List<string> problems) FindModParts(string modPath)
    {
        List<RegisteredPart> parts = [];
        List<string> problems = [];

        var modRulesFile = Path.Combine(modPath, "mod.rules");
        if (!File.Exists(modRulesFile))
            problems.Add("No mod.rules");
        else if (Load(modRulesFile, problems) is { } modRules)
        {
            if (modRules.TryGetNode("Actions", out IOTNode? actions))
                CollectActions(actions!, parts, problems, []);
            else
                problems.Add("mod.rules has no Actions");
        }

        return (Distinct(parts), problems);
    }

    private static void CollectActions(IOTNode actionsNode, List<RegisteredPart> parts, List<string> problems, HashSet<string> visited)
    {
        if (RulesText.Dereference(actionsNode) is not OTListNode actions || !visited.Add(actions.PathWithFile)) return;

        foreach (var child in actions.GetChildren())
            switch (RulesText.Dereference(child))
            {
                case OTListNode includedActions:
                    CollectActions(includedActions, parts, problems, visited);
                    break;
                case OTGroupNode action:
                    CollectAction(action, parts, problems);
                    break;
            }
    }

    private static void CollectAction(OTGroupNode action, List<RegisteredPart> parts, List<string> problems)
    {
        if (GetField(action, "AddTo") is not { } addTo || !IsPartsList(addTo)) return;

        switch (GetField(action, "Action"))
        {
            case "Add" when action.TryGetNode("ToAdd", out IOTNode? toAdd):
                AddPart(toAdd!, parts, problems);
                break;
            case "AddMany" when action.TryGetNode("ManyToAdd", out IOTNode? manyToAdd) && RulesText.Dereference(manyToAdd!) is OTListNode list:
                foreach (var entry in list.GetChildren())
                    AddPart(entry, parts, problems);
                break;
        }
    }

    // Mods aren't consistent with casing, e.g. "<ships/terran/terran.rules>/terran/Parts"
    private static bool IsPartsList(string path) => path.Split('/')[^1].Equals(PartsListName, StringComparison.OrdinalIgnoreCase);

    private static void AddPart(IOTNode entry, List<RegisteredPart> parts, List<string> problems)
    {
        try
        {
            switch (RulesText.Dereference(entry))
            {
                case OTGroupNode part when part.PathInFile.Trim('/') == "Part" && part.FileRoot?.FilePath is { } filePath:
                    parts.Add(new RegisteredPart(GetPathAsOnDisk(Path.GetFullPath(filePath.ToString())), part));
                    break;
                case OTGroupNode part:
                    // The override templates target <file>/Part, anything else can't be overridden by them
                    problems.Add($"{part.PathWithFile}: registered part isn't a top level 'Part' node");
                    break;
                case OTReferenceNode reference:
                    problems.Add($"{entry.PathAndPosWithFile}: can't resolve {reference.Target}");
                    break;
            }
        }
        catch (OTNavigateException e)
        {
            problems.Add($"{entry.PathAndPosWithFile}: {e.Message}");
        }
    }

    /// <summary>
    ///     The path comes from the reference as the mod wrote it, e.g. <c>dpmqmining_laser_small</c> for a file named <c>dpmQmining_laser_small</c>.
    ///     Windows doesn't care, but the generated actions should point at the file as it is named.
    /// </summary>
    private static string GetPathAsOnDisk(string path)
    {
        if (Path.GetDirectoryName(path) is not { } directory) return path;

        var parent = GetPathAsOnDisk(directory);
        var name = Path.GetFileName(path);
        var entry = Directory.EnumerateFileSystemEntries(parent, name, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
        return entry ?? Path.Combine(parent, name);
    }

    private static string? GetField(OTGroupNode group, string name) =>
        group.TryGetNode(name, out IOTNode? node) && RulesText.Dereference(node!) is OTFieldNode field ? field.Value : null;

    private static OTFile? Load(string file, List<string> problems)
    {
        try
        {
            return new OTFile(file);
        }
        catch (OTParseException e)
        {
            problems.Add($"Parse error in {file}: {e.Message}");
            return null;
        }
    }

    // A part can be registered more than once, e.g. for multiple factions
    private static List<RegisteredPart> Distinct(List<RegisteredPart> parts) => parts.DistinctBy(x => x.File, StringComparer.OrdinalIgnoreCase).ToList();
}
