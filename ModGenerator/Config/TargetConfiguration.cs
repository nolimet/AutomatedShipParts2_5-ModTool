using Newtonsoft.Json;

namespace ModGenerator.Config;

public class TargetConfiguration
{
    public readonly string BaseGamePath;
    public readonly string[] VanillaBlackList;

    public readonly string BaseModPath;
    public readonly Dictionary<long, string[]> TargetComponents;

    [JsonConstructor]
    public TargetConfiguration(string baseGamePath, string[] vanillaBlackList, string baseModPath, Dictionary<long, string[]> targetComponents)
    {
        BaseGamePath = baseGamePath;
        VanillaBlackList = vanillaBlackList;
        BaseModPath = baseModPath;
        TargetComponents = targetComponents;
    }

    public TargetConfiguration() : this(string.Empty, [], string.Empty, new Dictionary<long, string[]>()) { }
}
