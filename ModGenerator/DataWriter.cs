using ModGenerator.Data;
using ModGenerator.Helpers;
using Spectre.Console;

namespace ModGenerator;

public class DataWriter : IDisposable, IAsyncDisposable
{
    private bool _disposed;

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

    public void WriteVanillaData(string basePath)
    {
        var vanillaBasePAth = Path.Combine(BasePath, "vanilla");
        Directory.CreateDirectory(vanillaBasePAth);
        var (parts, report) = PartHelper.GetParts(basePath);
        AnsiConsole.WriteLine($"Found {parts.Count} vanilla parts");
        AnsiConsole.Write(report);
        foreach (var (path, crewData) in parts)
        {
            var partName = path.Split('/')[^1].Split('\\')[^1];
            Dictionary<string, string> actionReplacements = new()
            {
                { "PartName", path.Split('/')[^1].Split('\\')[^1] },
                { "PartPath", string.Join('/', path.Replace(basePath, string.Empty).Split('\\')[1..]) },
                { "CrewCount", crewData.CrewCount }
            };
            _modRulesWriter.WriteLine(FillTemplate(TemplateStorage.ActionTemplateVanilla, actionReplacements));

            CreateOverride(vanillaBasePAth, partName, crewData);
        }
    }

    public void WriteModData(string basePath, ulong modId)
    {
        if (ModInfoHelper.GetModInfo(basePath) is not { } modInfo)
        {
            AnsiConsole.MarkupLine($"[red]ModInfo is null![/] Cannot be null! Skipping {modId} at {basePath}");
            return;
        }

        var modBasePath = Path.Combine(BasePath, modId.ToString());
        Directory.CreateDirectory(modBasePath);

        var (parts, report) = PartHelper.GetParts(basePath);
        Console.WriteLine($"Found {parts.Count} for mod {modId} with name {modInfo.Name}, version {modInfo.Version}, game version {modInfo.GameVersion}");
        AnsiConsole.Write(report);

        _modRulesWriter.WriteLine();
        _modRulesWriter.WriteLine($"\t//{modInfo.Name}, version {modInfo.Version}, game version {modInfo.GameVersion}");

        foreach (var (path, crewData) in parts)
        {
            var partName = path.Split('/')[^1].Split('\\')[^1];
            Dictionary<string, string> actionReplacements = new()
            {
                { "PartName", partName },
                { "PartPath", string.Join('/', path.Replace(basePath, string.Empty).Split('\\')[1..]) },
                { "CrewCount", crewData.CrewCount },
                { "ModID", modId.ToString() },
                { "OverrideRulePath", Path.Combine(modId.ToString(), partName) }
            };

            _modRulesWriter.WriteLine(FillTemplate(TemplateStorage.ActionTemplateModded, actionReplacements));
            CreateOverride(modBasePath, partName, crewData);
        }

        Console.WriteLine();
    }

    private void CreateOverride(string basePath, string partName, CrewData crewData)
    {
        var crewCount = int.Parse(crewData.CrewCount);
        var partOverride = File.CreateText(Path.Combine(basePath, partName));
        List<string> crewToggleNames = new();
        for (var i = 0; i < crewCount; i++) crewToggleNames.Add($"PartCrewMinus{NumberToText.Convert(i)}");

        Dictionary<string, string> partReplacements = new()
        {
            { "CrewCount", crewData.CrewCount },
            { "CrewDestinations", crewData.Destinations },
            { "CrewLocations", crewData.Locations },
            { "DefaultPriority", crewData.DefaultPriority },
            { "PrerequisitesBeforeCrewing", crewData.CrewingPrerequisites },
            { "HighPriorityPrerequisites", crewData.HighPriorityPrerequisites },
            { "TogglesMinusNone", string.Join(", ", crewToggleNames[1..]) },
            { "CrewToggles", string.Join(", ", crewToggleNames) }
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
        if (_disposed) return;
        _disposed = true;

        try
        {
            // Flush is optional because Dispose flushes; keep it if you want best-effort sync flush.
            _modRulesWriter.Flush();
        }
        catch
        {
            // Swallow or log; don't let flushing errors prevent resource release.
        }
        finally
        {
            _modRulesWriter.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            await _modRulesWriter.FlushAsync().ConfigureAwait(false);
        }
        catch
        {
            // Swallow or log.
        }
        finally
        {
            await _modRulesWriter.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
