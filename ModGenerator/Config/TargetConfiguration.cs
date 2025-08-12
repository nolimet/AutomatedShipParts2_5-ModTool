using Newtonsoft.Json;

namespace ModGenerator.Config;

[method: JsonConstructor]
public class TargetConfiguration(string? baseGamePath, ulong[]? mods = null, Dictionary<ulong, string[]>? ignoredParts = null)
{
    public readonly string BaseGamePath = baseGamePath ?? string.Empty;
    public readonly ulong[] Mods = mods ?? GetDefaultMods();
    public readonly Dictionary<ulong, string[]> IgnoredParts = ignoredParts ?? GetDefaultIgnoredParts();

    private static ulong[] GetDefaultMods() =>
    [
        2888343841,
        2886141879,
        3052680147,
        3121346591,
        3119349707
    ];

    private static Dictionary<ulong, string[]> GetDefaultIgnoredParts() => new();
}
