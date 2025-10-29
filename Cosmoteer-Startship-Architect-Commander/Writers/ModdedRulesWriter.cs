using Cosmoteer.Data;
using Cosmoteer.Helpers;
using Spectre.Console;

namespace Cosmoteer.Writers;

public class ModdedRulesWriter(string modPath, ulong modId, IReadOnlyList<string>? ignoredParts, string path) : BaseRulesWriter(path)
{
    private const string ModFolderName = "mods";
    private static readonly string ModsPath = Path.Combine(BasePath, ModFolderName);

    private ModInfo? _modInfo;

    public override void Init()
    {
        if (ModInfoHelper.GetModInfo(modPath, modId) is not { } modInfo)
        {
            AnsiConsole.MarkupLine($"[red]ModInfo is null![/] Cannot be null! Skipping {modId} at {modPath}");
            return;
        }

        _modInfo = modInfo;

        Writer.Write(FillTemplate(TemplateStorage.ModRuleFileBase, new Dictionary<string, string>
        {
            { "ModId", modId.ToString() },
            { "ModName", modInfo.Name },
            { "ModVersion", modInfo.Version }
        }));
    }

    public void WriteModData()
    {
        if (_modInfo is not { } modInfo) return;

        var modBasePath = Path.Combine(ModsPath, modId.ToString());
        Directory.CreateDirectory(modBasePath);

        var (parts, report, issueCount) = PartHelper.GetParts(modPath, ignoredParts);
        Console.WriteLine($"Found {parts.Count} valid parts of which {issueCount} has issues");
        if (report.Rows.Count > 1)
            AnsiConsole.Write(report);

        Writer.WriteLine();
        Writer.WriteLine($"\t//{modInfo.Name}, version {modInfo.Version}, game version {modInfo.GameVersion}");

        foreach (var (path, crewData) in parts)
        {
            var partName = path.Split('/')[^1].Split('\\')[^1];
            Dictionary<string, string> actionReplacements = new()
            {
                { "PartName", partName },
                { "PartPath", string.Join('/', path.Replace(modPath, string.Empty).Split('\\')[1..]) },
                { "CrewCount", crewData.CrewCount },
                { "ModID", modId.ToString() },
                { "OverrideRulePath", partName /*Path.Combine(ModFolderName, modId.ToString(), partName)*/ }
            };

            Writer.WriteLine(FillTemplate(TemplateStorage.ActionTemplateModded, actionReplacements));
            CreateOverride(modBasePath, partName, crewData);
        }

        Console.WriteLine();
    }
}
