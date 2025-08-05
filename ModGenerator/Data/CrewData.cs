namespace ModGenerator.Data;

public readonly struct CrewData(string locations, string destinations, string crewCount)
{
    public readonly string Locations = locations;
    public readonly string Destinations = destinations;
    public readonly string CrewCount = crewCount;

    public override string ToString() => $"{Locations}\n{Destinations}\n{CrewCount}";
}
