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

    /// <param name="File">Per file: the comment above its actions and <c>{CrewActions}</c></param>
    protected readonly record struct ActionTemplates(string File, string Remove, string Replace, string AddBase);

    /// <summary>
    ///     Writes the actions for every file with crew to override and the override files themselves.
    ///     Per file the original crew components are removed, the values referring to their crew replaced (see <see cref="PartCrewData.Replacements" />)
    ///     and the overrides added as base.
    /// </summary>
    /// <param name="getPartPath">The path of a file as the actions refer to it</param>
    /// <param name="actionReplacements">Extra values for the action templates, e.g. the mod id</param>
    protected void WriteCrewOverrides(PartCrewData crew, string overrideFolder, ActionTemplates templates, Func<string, string> getPartPath, IReadOnlyDictionary<string, string> actionReplacements)
    {
        HashSet<string> usedOverrideNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (var file in crew.Targets.Select(x => x.File).Concat(crew.Replacements.Select(x => x.File)).Distinct())
        {
            var partPath = getPartPath(file);
            Dictionary<string, string> fileReplacements = new(actionReplacements)
            {
                { "PartName", Path.GetFileName(file) },
                { "PartPath", partPath }
            };

            List<string> removes = [];
            List<string> addBases = [];

            foreach (var target in crew.Targets.Where(x => x.File == file))
            {
                var overrideName = GetOverrideName(partPath, target.ComponentsPath, usedOverrideNames);
                Dictionary<string, string> targetReplacements = new(fileReplacements)
                {
                    { "ComponentsPath", target.ComponentsPath },
                    { "OverrideRulePath", overrideName }
                };

                removes.AddRange(target.CrewComponents.Select(x => FillTemplate(templates.Remove, new Dictionary<string, string>(targetReplacements) { { "ComponentName", x.ComponentName } })));
                addBases.Add(FillTemplate(templates.AddBase, targetReplacements));
                CreateOverride(overrideFolder, overrideName, target.CrewComponents);
            }

            var replaces = crew.Replacements
                .Where(x => x.File == file)
                .Select(x => FillTemplate(templates.Replace, new Dictionary<string, string>(fileReplacements) { { "ValuePath", x.Path }, { "Value", x.Value } }));

            fileReplacements["CrewActions"] = string.Join(Environment.NewLine, [..removes, ..replaces, ..addBases]);
            Writer.WriteLine(FillTemplate(templates.File, fileReplacements));
        }
    }

    /// <summary>
    ///     The file name of the part, as the overrides were always named.
    ///     Only when that's taken (two files with the same name, or a base file with more than one component group) the folders and group are added to keep it unique.
    /// </summary>
    private static string GetOverrideName(string partPath, string componentsPath, HashSet<string> usedNames)
    {
        var name = partPath.Split('/')[^1];
        if (componentsPath != "Part/Components") name = $"{Path.GetFileNameWithoutExtension(name)}_{componentsPath.Replace('/', '_')}.rules";
        if (!usedNames.Add(name))
        {
            name = $"{Path.ChangeExtension(partPath, null).Replace('/', '_')}_{componentsPath.Replace('/', '_')}.rules";
            usedNames.Add(name);
        }

        return name;
    }

    /// <summary>
    ///     Writes the override for every PartCrew component in one component group into one file.
    ///     Parts like the manipulator beam split their crew over multiple components (PartCrew1..4, one crew each),
    ///     the buff values continue over the components so the buff still removes one crew per level: buff 1 frees PartCrew1, buff 2 also PartCrew2, etc.
    /// </summary>
    private void CreateOverride(string basePath, string overrideName, IReadOnlyList<CrewData> crewComponents)
    {
        var partOverride = File.CreateText(Path.Combine(basePath, overrideName));

        // All crew components of a part share one priority, like the original components do, unless the part gave them their own key
        var defaultPriorityKey = $"{crewComponents[0].ComponentName}MinusNone";
        var buffOffset = 0;

        foreach (var crewData in crewComponents)
        {
            WriteComponentOverride(partOverride, crewData, buffOffset, crewData.PriorityKey ?? defaultPriorityKey);
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
            { "PrerequisitesBeforeCrewing", string.Concat(crewData.CrewingPrerequisites.Select(x => $"{x}, ")) },
            { "Fields", string.Join("\n\t", crewData.Fields) },
            { "TogglesMinusNone", string.Join(", ", crewToggleNames[1..]) },
            { "CrewToggles", string.Join(", ", crewToggleNames) },
            { "PriorityKey", priorityKey },
            // A component is its own priority key by default, so it only has to be written when it's shared with another one
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
