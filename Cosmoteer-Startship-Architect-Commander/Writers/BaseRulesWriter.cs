using Cosmoteer.Data;
using Cosmoteer.Helpers;

namespace Cosmoteer.Writers;

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

    protected static string GetTotalCrewCount(IReadOnlyList<CrewData> crewComponents) => crewComponents.Sum(x => int.Parse(x.CrewCount)).ToString();

    protected string GetRemoveActions(string template, IReadOnlyList<CrewData> crewComponents, Dictionary<string, string> actionReplacements) =>
        string.Join(Environment.NewLine, crewComponents.Select(x => FillTemplate(template, new Dictionary<string, string>(actionReplacements) { { "ComponentName", x.ComponentName } })));

    /// <summary>
    ///     Writes the override for every PartCrew component of a part into one file.
    ///     Parts like the manipulator beam split their crew over multiple components (PartCrew1..4, one crew each),
    ///     the buff values continue over the components so the buff still removes one crew per level: buff 1 frees PartCrew1, buff 2 also PartCrew2, etc.
    /// </summary>
    protected void CreateOverride(string basePath, string partName, IReadOnlyList<CrewData> crewComponents)
    {
        var partOverride = File.CreateText(Path.Combine(basePath, partName));

        // All crew components of a part share one priority, like the original components do
        var priorityKey = $"{crewComponents[0].ComponentName}MinusNone";
        var buffOffset = 0;

        foreach (var crewData in crewComponents)
        {
            WriteComponentOverride(partOverride, crewData, buffOffset, priorityKey);
            buffOffset += int.Parse(crewData.CrewCount);
        }

        partOverride.Flush();
        partOverride.Close();
    }

    private void WriteComponentOverride(StreamWriter partOverride, CrewData crewData, int buffOffset, string priorityKey)
    {
        var crewCount = int.Parse(crewData.CrewCount);
        List<string> crewToggleNames = [];

        for (var i = 0; i <= crewCount; i++) crewToggleNames.Add($"{crewData.ComponentName}Minus{NumberToText.Convert(i)}");

        var minusNoneName = crewToggleNames[0];

        Dictionary<string, string> partReplacements = new()
        {
            { "ComponentName", crewData.ComponentName },
            { "CrewCount", crewData.CrewCount },
            { "CrewDestinations", FormatCrewLocation(crewData.Destinations) },
            { "CrewLocations", FormatCrewLocation(crewData.Locations) },
            { "DefaultPriority", crewData.DefaultPriority },
            { "PrerequisitesBeforeCrewing", crewData.CrewingPrerequisites },
            { "HighPriorityPrerequisites", crewData.HighPriorityPrerequisites },
            { "TogglesMinusNone", string.Join(", ", crewToggleNames[1..]) },
            { "CrewToggles", string.Join(", ", crewToggleNames) },
            { "PriorityKey", priorityKey },
            { "MinusNonePriorityKey", minusNoneName == priorityKey ? string.Empty : $"OverridePriorityKey = {priorityKey}" }
        };

        partOverride.WriteLine(FillTemplate(TemplateStorage.PartTemplateBase, partReplacements));

        for (var i = 1; i <= crewCount - 1; i++)
        {
            partReplacements["CounterName"] = NumberToText.Convert(i);
            partReplacements["Counter"] = (buffOffset + i).ToString();
            partReplacements["RequiredCrew"] = Math.Max(0, crewCount - i).ToString();
            partOverride.WriteLine(FillTemplate(TemplateStorage.PartTemplateRepeat, partReplacements));
        }

        partReplacements["CounterName"] = NumberToText.Convert(crewCount);
        partReplacements["Counter"] = (buffOffset + crewCount).ToString();
        partOverride.WriteLine(FillTemplate(TemplateStorage.PartTemplateFinal, partReplacements));
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
