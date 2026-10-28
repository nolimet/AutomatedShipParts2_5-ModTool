namespace Cosmoteer.Data;

/// <param name="componentName">The name of the PartCrew component in the part, e.g. <c>PartCrew</c> or <c>PartCrew1</c></param>
/// <param name="crewingPrerequisites">The original prerequisites, the overrides add their own toggle to these</param>
/// <param name="priorityKey">The original OverridePriorityKey, or null if the component didn't have one</param>
/// <param name="fields">Every other field of the component as <c>Name = value</c>, so nothing the part sets is lost in the override</param>
public readonly struct CrewData(string componentName, string crewCount, IReadOnlyList<string> crewingPrerequisites, string? priorityKey, IReadOnlyList<string> fields)
{
    public readonly string ComponentName = componentName;
    public readonly string CrewCount = crewCount;
    public readonly IReadOnlyList<string> CrewingPrerequisites = crewingPrerequisites;
    public readonly string? PriorityKey = priorityKey;
    public readonly IReadOnlyList<string> Fields = fields;

    public override string ToString() => $"{ComponentName}\n{CrewCount}\n{string.Join('\n', Fields)}";
}
