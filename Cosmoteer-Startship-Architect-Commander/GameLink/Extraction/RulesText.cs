using System.Text.RegularExpressions;
using Halfling.ObjectText;

namespace Cosmoteer.GameLink.Extraction;

/// <summary>
///     Turns parsed ObjectText nodes back into .rules text, so values can be written into the override templates.
/// </summary>
public static class RulesText
{
    /// <summary>
    ///     Writes a node as it would appear on the right side of <c>Name = ...</c>:
    ///     fields as their value, references as <c>&amp;target</c>, lists as <c>[...]</c> and groups as <c>{...}</c>.
    /// </summary>
    public static string ToRulesText(IOTNode node) => node switch
    {
        OTFieldNode field => field.PreferQuotedValue ? $"\"{field.Value}\"" : field.Value,
        OTReferenceNode reference => $"&{reference.Target}",
        OTListNode list => $"[{list.GetBodyText().Trim()}]",
        OTGroupNode group => $"{{{group.GetBodyText().Trim()}}}",
        _ => node.GetSignificantText().Trim()
    };

    /// <summary>
    ///     Follows a reference to the node it points at, e.g. <c>&amp;../../CrewLocation/Location</c> becomes the <c>[x, y]</c> list it refers to.
    ///     References from the game root (<c>&amp;/PRIORITIES/...</c>) can't be followed outside the game, those are returned as-is.
    /// </summary>
    public static IOTNode Dereference(IOTNode node)
    {
        if (node is not OTReferenceNode reference || IsGameRootReference(reference)) return node;
        return reference.TryFindFinalTarget(out var target) ? target : node;
    }

    public static bool IsGameRootReference(OTReferenceNode reference) => reference.Target.StartsWith('/');

    /// <summary>
    ///     A reference that points at the same node from any file: from the game root (<c>&amp;/PRIORITIES/...</c>) or to a file from the game folder (<c>&amp;&lt;./Data/...&gt;/...</c>).
    ///     Other file paths (<c>&amp;&lt;../priorities.rules&gt;</c>) are relative to the file that contains them, so they break once copied into the override.
    /// </summary>
    public static bool IsPortableTarget(string target) => target.StartsWith('/') || target.StartsWith("<./");

    // A reference inside an expression, e.g. the &~/Part/Components/PartCrew/Crew in (&~/Part/Components/PartCrew/Crew) * 2
    public static readonly Regex ExpressionReference = new(@"&(?<target>(?:<[^>]*>)?[^\s()*+\-,;\]}]+)", RegexOptions.Compiled);

    /// <summary>
    ///     Writes a node so it means the same when it's copied into another file:
    ///     references that are only valid from where they're written (relative, <c>~</c> or a relative file path) are replaced by the value they point at,
    ///     and inherited groups and lists are written out in full.
    /// </summary>
    /// <returns>The text, or null if the node can't be made portable, the reason is added to <paramref name="problems" /></returns>
    public static string? ToPortableText(IOTNode node, List<string> problems)
    {
        switch (node)
        {
            case OTReferenceNode reference when IsPortableTarget(reference.Target):
                return $"&{reference.Target}";
            case OTReferenceNode reference:
                if (reference.TryFindFinalTarget(out var target)) return ToPortableText(target, problems);
                problems.Add($"Can't resolve {reference.Target}");
                return null;
            case OTListNode list:
                var items = list.GetChildren().Select(x => ToPortableText(x, problems)).ToList();
                return items.Any(x => x == null) ? null : $"[{string.Join(", ", items)}]";
            case OTGroupNode group:
                var fields = group.Names.Distinct().Select(x => group.TryGetNode(x, out IOTNode? child) ? ToPortableText(child!, problems) is { } text ? $"{x} = {text}" : null : null).ToList();
                return fields.Any(x => x == null) ? null : $"{{ {string.Join("; ", fields)} }}";
        }

        var value = ToRulesText(node);

        // An expression like (&../Crew) * 2 can't be resolved here, so it's only copied when all of its references work from anywhere
        if (ExpressionReference.Matches(value).Any(x => !IsPortableTarget(x.Groups["target"].Value)))
        {
            problems.Add($"Expression with a relative reference: {value}");
            return null;
        }

        return value;
    }
}
