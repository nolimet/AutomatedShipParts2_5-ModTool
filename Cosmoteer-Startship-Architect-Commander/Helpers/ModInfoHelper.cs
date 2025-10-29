using System.Text.RegularExpressions;
using ModGenerator.Data;
using Spectre.Console;

namespace ModGenerator.Helpers;

public static class ModInfoHelper
{
    private static readonly Regex NameRegex = new(@"Name = (.+)");
    private static readonly Regex VersionRegex = new(@"Version = (.*)");
    private static readonly Regex GameVersionRegex = new(@"CompatibleGameVersions = \[((?:""?\w+\.\w+\.\w+""?,? ?)+)\]");

    public static ModInfo? GetModInfo(string path, ulong modId)
    {
        var modRulesPath = Path.Combine(path, "mod.rules");
        if (!File.Exists(modRulesPath)) return null;

        var data = File.ReadAllText(modRulesPath);
        var nameResult = NameRegex.Match(data);
        var versionResult = VersionRegex.Match(data);
        var gameVersionResult = GameVersionRegex.Match(data);

        string? gameVersion = null;
        if (gameVersionResult.Success)
        {
            var tmpVersionText = gameVersionResult.Groups[1].Value.Trim().Trim('"').RemoveMarkup();
            if (tmpVersionText.Contains(','))
            {
                var split = tmpVersionText.Split(',');
                var highestVersion = split[0];
                foreach (var s in split)
                {
                    var splitVersion = s.Trim().Trim('"').RemoveMarkup().Split(".");
                    var major = int.TryParse(splitVersion[0], out var majorResult) ? majorResult : -1;
                    var minor = int.TryParse(splitVersion[1], out var minorResult) ? minorResult : -1;
                    var patch = int.TryParse(splitVersion[2], out var patchResult) ? patchResult : -1;
                    if (major > majorResult || (major == majorResult && minor > minorResult) || (major == majorResult && minor == minorResult && patch > patchResult))
                        highestVersion = s;
                }

                gameVersion = highestVersion.Replace("\"", "");
            }
            else
                gameVersion = tmpVersionText.Replace("\"", "");
        }

        var g = new Grid();
        g.AddColumns(4);
        g.AddRow("ModName", "Version", "GameVersion", "ModId");
        g.AddRow
        (
            nameResult.Success ? nameResult.Groups[1].Value.Trim().Trim('"').RemoveMarkup() : "null",
            versionResult.Success ? versionResult.Groups[1].Value.Trim().Trim('"').RemoveMarkup() : "null",
            gameVersion ?? "null",
            modId.ToString()
        );

        AnsiConsole.Write(g);

        if (nameResult.Success && versionResult.Success && gameVersionResult.Success)
            return new ModInfo(nameResult.Groups[1].Value.Trim().Trim('"'), versionResult.Groups[1].Value.Trim().Trim('"'), gameVersion);
        return null;
    }
}
