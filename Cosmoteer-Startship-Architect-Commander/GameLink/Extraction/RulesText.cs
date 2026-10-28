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
    ///     Writes every item of a list, including items inherited from base lists, one per line.
    /// </summary>
    public static string ListItems(OTParentNode list, bool dereference, string separator) =>
        string.Join(separator, list.GetChildren().Select(x => ToRulesText(dereference ? Dereference(x) : x)));
}
