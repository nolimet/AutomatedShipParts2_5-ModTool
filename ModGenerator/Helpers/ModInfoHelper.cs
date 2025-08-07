using System.Text.RegularExpressions;
using ModGenerator.Data;

namespace ModGenerator.Helpers;

public static class ModInfoHelper
{
    private static readonly Regex NameRegex = new(@"Name = (.+)");
    private static readonly Regex VersionRegex = new(@"Version = (\d+\.\d+\.\d+)");
    private static readonly Regex GameVersionRegex = new(@"CompatibleGameVersions = \[""(\d+\.\d+\.\d+)\""]");

    public static ModInfo? GetModInfo(string path)
    {
        var modRulesPath = Path.Combine(path, "mod.rules");
        if (!File.Exists(modRulesPath)) return null;

        var data = File.ReadAllText(modRulesPath);
        var nameResult = NameRegex.Match(data);
        var versionResult = VersionRegex.Match(data);
        var gameVersionResult = GameVersionRegex.Match(data);

        if (nameResult.Success && versionResult.Success && gameVersionResult.Success)
            return new ModInfo(nameResult.Groups[1].Value, versionResult.Groups[1].Value, gameVersionResult.Groups[1].Value);

        Console.WriteLine($"Failed to find mod info at {modRulesPath}");
        return null;
    }
}
