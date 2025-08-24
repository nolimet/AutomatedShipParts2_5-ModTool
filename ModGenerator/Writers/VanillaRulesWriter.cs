using ModGenerator.Helpers;
using Spectre.Console;

namespace ModGenerator.Writers;

public class VanillaRulesWriter(string path) : BaseRulesWriter(path)
{
    public override void Init()
    {
        Writer.Write(FillTemplate(TemplateStorage.ModRuleFileBase, new Dictionary<string, string>
        {
            { "ModId", "-1" },
            { "ModName", "Vanilla" },
            { "ModVersion", "" }
        }));
    }

    public void WriteVanillaData(string basePath)
    {
        var vanillaBasePAth = Path.Combine(BasePath, "vanilla");
        Directory.CreateDirectory(vanillaBasePAth);
        var (parts, report) = PartHelper.GetParts(basePath);
        AnsiConsole.WriteLine($"Found {parts.Count} vanilla parts");
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
