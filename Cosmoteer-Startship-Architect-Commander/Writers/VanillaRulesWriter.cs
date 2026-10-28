using Cosmoteer.GameLink;
using Spectre.Console;

namespace Cosmoteer.Writers;

public class VanillaRulesWriter(string path) : BaseRulesWriter(path)
{
    private static readonly ActionTemplates Templates = new(
        TemplateStorage.ActionTemplateVanilla,
        TemplateStorage.ActionTemplateVanillaRemove,
        TemplateStorage.ActionTemplateVanillaReplace,
        TemplateStorage.ActionTemplateVanillaAddBase
    );

    public override void Init()
    {
        Writer.Write(FillTemplate(TemplateStorage.ModRuleFileBase, new Dictionary<string, string>
        {
            { "ModId", "" },
            { "ModName", "Cosmoteer Base" },
            { "ModVersion", "" }
        }));
    }

    public void WriteVanillaData(string basePath)
    {
        var vanillaBasePAth = Path.Combine(BasePath, "vanilla");
        Directory.CreateDirectory(vanillaBasePAth);
        var (crew, partCount, report, issueCount) = PartSource.GetVanillaParts(basePath);
        AnsiConsole.WriteLine($"Found {partCount} vanilla parts of which {issueCount} has issues");
        if (report.Rows.Count > 1)
            AnsiConsole.Write(report);

        WriteCrewOverrides(crew, vanillaBasePAth, Templates, path => string.Join('/', path.Replace(basePath, string.Empty).Split('\\')[2..]), new Dictionary<string, string>());

        Console.WriteLine();
    }
}
