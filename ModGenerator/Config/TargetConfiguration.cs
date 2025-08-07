using Newtonsoft.Json;

namespace ModGenerator.Config;

[method: JsonConstructor]
public class TargetConfiguration(string? baseGamePath, List<ulong>? mods)
{
    public readonly string BaseGamePath = baseGamePath ?? string.Empty;
    public readonly List<ulong> Mods = mods ?? [];

    public TargetConfiguration(string baseGamePath) : this(baseGamePath, GetDefaultMods()) { }

    private static List<ulong> GetDefaultMods() =>
    [
        2888343841
    ];
}
