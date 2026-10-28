namespace Cosmoteer.Data;

public readonly struct CrewData(string componentName, string locations, string destinations, string crewCount, string defaultPriority, string crewingPrerequisites, string highPriorityPrerequisites)
{
    /// <summary>The name of the PartCrew component in the part, e.g. <c>PartCrew</c> or <c>PartCrew1</c></summary>
    public readonly string ComponentName = componentName;
    public readonly string Locations = locations;
    public readonly string Destinations = destinations;
    public readonly string CrewCount = crewCount;
    public readonly string DefaultPriority = defaultPriority;
    public readonly string CrewingPrerequisites = crewingPrerequisites;
    public readonly string HighPriorityPrerequisites = highPriorityPrerequisites;

    public override string ToString() => $"{ComponentName}\n{Locations}\n{Destinations}\n{CrewCount}";
}
