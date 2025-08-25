using ModGenerator.Data;
using ModGenerator.Helpers;

namespace ModGenerator.Writers;

public abstract class BaseRulesWriter : IDisposable, IAsyncDisposable
{
    protected static readonly string BasePath = Path.Combine(AppContext.BaseDirectory, "Output");
    protected readonly StreamWriter Writer;
    private bool _disposed;
    private readonly string _path;

    protected BaseRulesWriter(string path)
    {
        _path = path;
        var fullPath = Path.Combine(BasePath, path);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Directory!.Exists) fileInfo.Directory.Create();

        Writer = new StreamWriter(Path.Combine(BasePath, path));
    }

    protected virtual bool WriteClosingTag => true;

    public abstract void Init();

    public string GetRelativePath() => _path;

    protected void CreateOverride(string basePath, string partName, CrewData crewData)
    {
        var crewCount = int.Parse(crewData.CrewCount);
        var partOverride = File.CreateText(Path.Combine(basePath, partName));
        List<string> crewToggleNames = [];

        for (var i = 0; i <= crewCount; i++) crewToggleNames.Add($"PartCrewMinus{NumberToText.Convert(i)}");

        Dictionary<string, string> partReplacements = new()
        {
            {"CrewCount", crewData.CrewCount},
            {"CrewDestinations", FormatCrewLocation(crewData.Destinations)},
            {"CrewLocations", FormatCrewLocation(crewData.Locations)},
            {"DefaultPriority", crewData.DefaultPriority},
            {"PrerequisitesBeforeCrewing", crewData.CrewingPrerequisites},
            {"HighPriorityPrerequisites", crewData.HighPriorityPrerequisites},
            {"TogglesMinusNone", string.Join(", ", crewToggleNames[1..])},
            {"CrewToggles", string.Join(", ", crewToggleNames)}
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

    protected string FillTemplate(string template, Dictionary<string, string> replaceCommands)
    {
        foreach (var (key, value) in replaceCommands) template = template.Replace($"{{{key}}}", value);
        return template;
    }

    protected static string FormatCrewLocation(string input)
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
            if (WriteClosingTag)
                Writer.WriteLine("]");
            // Flush is optional because Dispose flushes; keep it if you want best-effort sync flush.
            Writer.Flush();
        }
        catch
        {
            // Swallow or log; don't let flushing errors prevent resource release.
        }
        finally
        {
            Writer.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (WriteClosingTag)
                await Writer.WriteLineAsync("]");
            await Writer.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            await Writer.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
