using Newtonsoft.Json;

namespace ModGenerator.Config;

[method: JsonConstructor]
[JsonObject(MemberSerialization.OptIn)]
public class TargetConfiguration(string? baseGamePath, ulong[]? mods = null, Dictionary<ulong, string[]>? ignoredParts = null)
{
    [JsonProperty] public readonly string BaseGamePath = baseGamePath ?? string.Empty;
    [JsonProperty] public readonly ulong[] Mods = mods ?? GetDefaultMods();
    [JsonProperty] public readonly Dictionary<ulong, string[]> IgnoredParts = ignoredParts ?? GetDefaultIgnoredParts();

    private static ulong[] GetDefaultMods() =>
    [
        2888343841,
        2886141879,
        3052680147,
        3121346591,
        3119349707,
        //3121346591,
        //3119349707,
        3539253648,
        2884747698,
        3310834040,
        2995534359,
        3541853292,
        3546356734
    ];

    private static Dictionary<ulong, string[]> GetDefaultIgnoredParts() => new()
    {
        { 3119349707, [".*base.rules"] }
    };
}
