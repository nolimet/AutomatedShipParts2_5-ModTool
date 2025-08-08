using System.Text.RegularExpressions;
using ModGenerator.Data;
using Spectre.Console;

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

        var g = new Grid();
        g.AddColumns(3);
        g.AddRow("ModName", "Version", "GameVersion");
        g.AddRow
        (
            nameResult.Success ? nameResult.Groups[1].Value.Trim().Trim('"').RemoveMarkup() : "null",
            versionResult.Success ? versionResult.Groups[1].Value.Trim().Trim('"').RemoveMarkup() : "null",
            gameVersionResult.Success ? gameVersionResult.Groups[1].Value.Trim().Trim('"').RemoveMarkup() : "null"
        );

        AnsiConsole.Write(g);

        if (nameResult.Success && versionResult.Success && gameVersionResult.Success)
            return new ModInfo(nameResult.Groups[1].Value.Trim().Trim('"'), versionResult.Groups[1].Value.Trim().Trim('"'), gameVersionResult.Groups[1].Value.Trim().Trim('"'));
        return null;
    }
}
