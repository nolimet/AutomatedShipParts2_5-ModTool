using ModGenerator.Data;
using ModGenerator.Helpers;
using Spectre.Console;

namespace ModGenerator;

public class DataWriter : IDisposable, IAsyncDisposable
{
    private readonly string BasePath = Path.Combine(AppContext.BaseDirectory, "Output");

    private readonly StreamWriter _modRulesWriter;

    public DataWriter()
    {
        if (Directory.Exists(BasePath))
            Directory.Delete(BasePath, true);
        Directory.CreateDirectory(BasePath);
        _modRulesWriter = new StreamWriter(Path.Combine(BasePath, "mod.rules"));
    }

    public void Init()
    {
        _modRulesWriter.Write(TemplateStorage.ModRulesBase);
    }

    private string FillTemplate(string template, Dictionary<string, string> replaceCommands)
    {
        foreach (var (key, value) in replaceCommands) template = template.Replace($"{{{key}}}", value);
        return template;
    }

    public void WriteVanillaData(string basePath, Dictionary<string, CrewData> parts)
    {
        var vanillaBasePAth = Path.Combine(BasePath, "vanilla");
        Directory.CreateDirectory(vanillaBasePAth);

        AnsiConsole.WriteLine($"Found {parts.Count} vanilla parts");
        foreach (var (path, crewData) in parts)
        {
            var partName = path.Split('/')[^1].Split('\\')[^1];
            Dictionary<string, string> actionReplacements = new()
            {
                {"PartName", path.Split('/')[^1].Split('\\')[^1]},
                {"PartPath", string.Join('/', path.Replace(basePath, string.Empty).Split('\\')[1..])},
                {"CrewCount", crewData.CrewCount}
            };
            _modRulesWriter.WriteLine(FillTemplate(TemplateStorage.ActionTemplateVanilla, actionReplacements));

            CreateOverride(vanillaBasePAth, partName, crewData);
        }
    }

    public void WriteModData(string basePath, ulong modId, Dictionary<string, CrewData> parts)
    {
        var modBasePath = Path.Combine(BasePath, modId.ToString());
        Directory.CreateDirectory(modBasePath);

        AnsiConsole.Write($"Found {parts.Count} parts for mod with id {modId}");
        _modRulesWriter.WriteLine($"\t//----{modId}----");
        foreach (var (path, crewData) in parts)
        {
            var partName = path.Split('/')[^1].Split('\\')[^1];
            Dictionary<string, string> actionReplacements = new()
            {
                {"PartName", partName},
                {"PartPath", string.Join('/', path.Replace(basePath, string.Empty).Split('\\')[1..])},
                {"CrewCount", crewData.CrewCount},
                {"ModID", modId.ToString()},
                {"OverrideRulePath", Path.Combine(modId.ToString(), partName)}
            };
            _modRulesWriter.WriteLine(FillTemplate(TemplateStorage.ActionTemplateModded, actionReplacements));

            CreateOverride(modBasePath, partName, crewData);
        }
    }

    private void CreateOverride(string basePath, string partName, CrewData crewData)
    {
        var crewCount = int.Parse(crewData.CrewCount);
        var partOverride = File.CreateText(Path.Combine(basePath, partName));
        List<string> crewToggleNames = new();
        for (var i = 0; i < crewCount; i++) crewToggleNames.Add($"PartCrewMinus{NumberToText.Convert(i)}");

        Dictionary<string, string> partReplacements = new()
        {
            {"CrewCount", crewData.CrewCount},
            {"CrewDestinations", crewData.Destinations},
            {"CrewLocations", crewData.Locations},
            {"DefaultPriority", crewData.DefaultPriority},
            {"PrerequisitesBeforeCrewing", crewData.CrewingPrerequisites},
            {"HighPriorityPrerequisites", crewData.HighPriorityPrerequisites},
            {"TogglesMinusNone", string.Join(", ", crewToggleNames[1..])},
            {"CrewToggles", string.Join(", ", crewToggleNames)}
        };

        partOverride.WriteLine(FillTemplate(TemplateStorage.PartTemplateBase, partReplacements));

        for (var i = 1; i < crewCount; i++)
        {
            partReplacements["CounterName"] = NumberToText.Convert(i);
            partReplacements["Counter"] = i.ToString();
            partOverride.WriteLine(FillTemplate(TemplateStorage.PartTemplateRepeat, partReplacements));
        }

        partOverride.Flush();
        partOverride.Close();
    }

    public void Dispose()
    {
        _modRulesWriter.Flush();
        _modRulesWriter.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _modRulesWriter.FlushAsync();
        await _modRulesWriter.DisposeAsync();
    }
}
