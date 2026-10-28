using Cosmoteer.Data;
using Cosmoteer.GameLink;
using Cosmoteer.Helpers;
using Spectre.Console;

namespace Cosmoteer.Writers;

public class ModdedRulesWriter(string modPath, string gamePath, ulong modId, IReadOnlyList<string>? ignoredParts, string path) : BaseRulesWriter(path)
{
    private const string ModFolderName = "mods";
    private static readonly string ModsPath = Path.Combine(BasePath, ModFolderName);

    private static readonly ActionTemplates Templates = new(
        TemplateStorage.ActionTemplateModded,
        TemplateStorage.ActionTemplateModdedRemove,
        TemplateStorage.ActionTemplateModdedReplace,
        TemplateStorage.ActionTemplateModdedAddBase
    );

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

        var (crew, partCount, report, issueCount) = PartSource.GetModParts(modPath, gamePath, ignoredParts);
        Console.WriteLine($"Found {partCount} valid parts of which {issueCount} has issues");
        if (report.Rows.Count > 1)
            AnsiConsole.Write(report);

        Writer.WriteLine();
        Writer.WriteLine($"\t//{modInfo.Name}, version {modInfo.Version}, game version {modInfo.GameVersion}");

        WriteCrewOverrides(crew, modBasePath, Templates, path => string.Join('/', path.Replace(modPath, string.Empty).Split('\\')[1..]),
            new Dictionary<string, string> { { "ModID", modId.ToString() } });

        Console.WriteLine();
    }
}
