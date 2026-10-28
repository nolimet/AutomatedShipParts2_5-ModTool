namespace Cosmoteer.Data;

/// <summary>
///     PartCrew components to override where they're defined. That isn't always the part's own file:
///     e.g. a mk2 part that inherits <c>&lt;mk1.rules&gt;/Part</c> gets its crew from mk1, so the override goes into mk1 and mk2 inherits it.
/// </summary>
/// <param name="file">The file the components are written in</param>
/// <param name="componentsPath">The group in that file holding the components, usually <c>Part/Components</c></param>
public readonly struct CrewOverrideTarget(string file, string componentsPath, IReadOnlyList<CrewData> crewComponents)
{
    public readonly string File = file;
    public readonly string ComponentsPath = componentsPath;
    public readonly IReadOnlyList<CrewData> CrewComponents = crewComponents;
}

/// <summary>A value that refers to the <c>Crew</c> of a removed PartCrew component, e.g. <c>Stats/CrewRequired</c>, and the value to replace it with.</summary>
/// <param name="file">The file the value is written in</param>
/// <param name="path">The path of the value inside the file, e.g. <c>Part/StatsByCategory/0/Stats/CrewRequired</c></param>
public readonly struct CrewReferenceReplacement(string file, string path, string value)
{
    public readonly string File = file;
    public readonly string Path = path;
    public readonly string Value = value;
}

/// <summary>Everything needed to override the crew of one or more parts.</summary>
public readonly struct PartCrewData(IReadOnlyList<CrewOverrideTarget> targets, IReadOnlyList<CrewReferenceReplacement> replacements)
{
    public readonly IReadOnlyList<CrewOverrideTarget> Targets = targets;
    public readonly IReadOnlyList<CrewReferenceReplacement> Replacements = replacements;
}
