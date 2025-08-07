using Newtonsoft.Json;

namespace ModGenerator.Config;

[method: JsonConstructor]
public class TargetConfiguration(string baseGamePath, string[] vanillaBlackList, Dictionary<ulong, string[]?> targetComponents)
{
    public readonly string BaseGamePath = baseGamePath;
    public readonly string[] VanillaBlackList = vanillaBlackList;

    public readonly Dictionary<ulong, string[]?> TargetComponents = targetComponents;

    public TargetConfiguration() : this("D:/SteamLibrary/steamapps/common/Cosmoteer", [], new Dictionary<ulong, string[]?> {{2888343841, null}}) { }

    private static Dictionary<ulong, string[]?> GetDefaultMods() =>
        new()
        {
            {2888343841, null}
        };
}
