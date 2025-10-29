using Newtonsoft.Json;

namespace Cosmoteer.Config;

[method: JsonConstructor]
[JsonObject(MemberSerialization.OptIn)]
public class TargetConfiguration(string? baseGamePath, ulong[]? mods = null, ulong[]? manualMods = null, Dictionary<ulong, string[]>? ignoredParts = null)
{
    [JsonProperty] public readonly string BaseGamePath = baseGamePath ?? string.Empty;
    [JsonProperty] public readonly ulong[] Mods = mods ?? GetDefaultMods();
    [JsonProperty] public readonly ulong[] ManualMods = manualMods ?? GetDefaultManualMods();
    [JsonProperty] public readonly Dictionary<ulong, string[]> IgnoredParts = ignoredParts ?? GetDefaultIgnoredParts();

    private static ulong[] GetDefaultMods() =>
    [
        2886141879, //[DIGI] Modular Missiles https://steamcommunity.com/sharedfiles/filedetails/?id=2886141879
        3052680147, //巨炮整合最终版0.22（More Deck Cannons for 0.22） https://steamcommunity.com/sharedfiles/filedetails/?id=3052680147
        //3121346591, //Star Wars: ACD Factions Add-on https://steamcommunity.com/sharedfiles/filedetails/?id=3121346591
        //3119349707, //Star Wars: A Cosmos Divided Main Mod https://steamcommunity.com/sharedfiles/filedetails/?id=3119349707
        3539253648, //巨炮0.30测试版(More Deck Cannons 0.30 Beta) https://steamcommunity.com/sharedfiles/filedetails/?id=3539253648
        2884747698, //Autocannon https://steamcommunity.com/sharedfiles/filedetails/?id=2884747698
        3310834040, //PreMeltdown. Crewed Power Distribution - ETTM https://steamcommunity.com/sharedfiles/filedetails/?id=3310834040
        2995534359, //Sunflower Corporation https://steamcommunity.com/sharedfiles/filedetails/?id=2995534359
        3541853292, //Diagonal Cockpits https://steamcommunity.com/sharedfiles/filedetails/?id=3541853292
        3546356734, //The Infernum https://steamcommunity.com/sharedfiles/filedetails/?id=3546356734
        2891248440, //[WIP]Weapon Turrets https://steamcommunity.com/sharedfiles/filedetails/?id=2891248440
        2899977331, //Ancient Singularity Cannon https://steamcommunity.com/sharedfiles/filedetails/?id=2897171599
        2897171599, //Ancient Phazor https://steamcommunity.com/sharedfiles/filedetails/?id=2899977331,
        2946411143 //Extended Tech Tree Mod. https://steamcommunity.com/sharedfiles/filedetails/?id=2946411143
    ];

    public static ulong[] GetDefaultManualMods() =>
    [
        2888343841, //Tiered Parts https://steamcommunity.com/sharedfiles/filedetails/?id=2888343841 (manual support)
    ];

    private static Dictionary<ulong, string[]> GetDefaultIgnoredParts() => new()
    {
        { 3119349707, [".*base.rules"] }
    };
}
