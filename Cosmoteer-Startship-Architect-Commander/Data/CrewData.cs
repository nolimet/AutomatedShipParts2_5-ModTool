namespace Cosmoteer.Data;

public readonly struct CrewData(string locations, string destinations, string crewCount, string defaultPriority, string crewingPrerequisites, string highPriorityPrerequisites)
{
    public readonly string Locations = locations;
    public readonly string Destinations = destinations;
    public readonly string CrewCount = crewCount;
    public readonly string DefaultPriority = defaultPriority;
    public readonly string CrewingPrerequisites = crewingPrerequisites;
    public readonly string HighPriorityPrerequisites = highPriorityPrerequisites;

    public override string ToString() => $"{Locations}\n{Destinations}\n{CrewCount}";
}
