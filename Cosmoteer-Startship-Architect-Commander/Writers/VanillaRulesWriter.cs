using Cosmoteer.Helpers;
using Spectre.Console;

namespace Cosmoteer.Writers;

public class VanillaRulesWriter(string path) : BaseRulesWriter(path)
{
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
        var (parts, report, issueCount) = PartHelper.GetParts(basePath);
        AnsiConsole.WriteLine($"Found {parts.Count} vanilla parts of which {issueCount} has issues");
        if (report.Rows.Count > 1)
            AnsiConsole.Write(report);

        foreach (var (path, crewData) in parts)
        {
            var partName = path.Split('/')[^1].Split('\\')[^1];
            Dictionary<string, string> actionReplacements = new()
            {
                { "PartName", path.Split('/')[^1].Split('\\')[^1] },
                { "PartPath", string.Join('/', path.Replace(basePath, string.Empty).Split('\\')[2..]) },
                { "CrewCount", crewData.CrewCount },
                { "OverrideRulePath", partName }
            };
            Writer.WriteLine(FillTemplate(TemplateStorage.ActionTemplateVanilla, actionReplacements));
            
            CreateOverride(vanillaBasePAth, partName, crewData);
        }

        Console.WriteLine();
    }
}
