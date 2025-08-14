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
                { "OverrideRulePath", Path.Combine("vanilla", partName) }
            };
            _modRulesWriter.WriteLine(FillTemplate(TemplateStorage.ActionTemplateVanilla, actionReplacements));

            CreateOverride(vanillaBasePAth, partName, crewData);
        }

        Console.WriteLine();
    }

    public void WriteModData(string basePath, ulong modId, IReadOnlyList<string>? ignoredParts)
    {
        if (ModInfoHelper.GetModInfo(basePath, modId) is not { } modInfo)
        {
            AnsiConsole.MarkupLine($"[red]ModInfo is null![/] Cannot be null! Skipping {modId} at {basePath}");
            return;
        }

        var modBasePath = Path.Combine(BasePath, modId.ToString());
        Directory.CreateDirectory(modBasePath);

        var (parts, report) = PartHelper.GetParts(basePath, ignoredParts);
        Console.WriteLine($"Found {parts.Count} valid parts");
        if (report.Rows.Count > 1)
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
        for (var i = 0; i <= crewCount; i++) crewToggleNames.Add($"PartCrewMinus{NumberToText.Convert(i)}");

        Dictionary<string, string> partReplacements = new()
        {
            { "CrewCount", crewData.CrewCount },
            { "CrewDestinations", FormatCrewLocation(crewData.Destinations) },
            { "CrewLocations", FormatCrewLocation(crewData.Locations) },
            { "DefaultPriority", crewData.DefaultPriority },
            { "PrerequisitesBeforeCrewing", crewData.CrewingPrerequisites },
            { "HighPriorityPrerequisites", crewData.HighPriorityPrerequisites },
            { "TogglesMinusNone", string.Join(", ", crewToggleNames[1..]) },
            { "CrewToggles", string.Join(", ", crewToggleNames) }
        };

        partOverride.WriteLine(FillTemplate(TemplateStorage.PartTemplateBase, partReplacements));

        for (var i = 1; i <= crewCount - 1; i++)
        {
            partReplacements["CounterName"] = NumberToText.Convert(i);
            partReplacements["Counter"] = i.ToString();
            partReplacements["RequiredCrew"] = Math.Max(0, crewCount - i).ToString();
            partOverride.WriteLine(FillTemplate(TemplateStorage.PartTemplateRepeat, partReplacements));
        }

        partReplacements["CounterName"] = NumberToText.Convert(crewCount);
        partReplacements["Counter"] = crewCount.ToString();
        partOverride.WriteLine(FillTemplate(TemplateStorage.PartTemplateFinal, partReplacements));
        ;

        partOverride.Flush();
        partOverride.Close();
    }

    private string FormatCrewLocation(string input)
    {
        var sanitized = input.ReplaceLineEndings().Replace("\t", string.Empty);
        return string.Join("\n\t\t", sanitized.Split('\n'));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _modRulesWriter.WriteLine("]");
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
            await _modRulesWriter.WriteLineAsync("]");
            await _modRulesWriter.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            await _modRulesWriter.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
