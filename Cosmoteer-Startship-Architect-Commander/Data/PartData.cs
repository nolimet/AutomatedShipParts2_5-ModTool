namespace Cosmoteer.Data;

public readonly struct PartData(CrewData crewData, ModInfo modInfo)
{
    public readonly CrewData CrewData = crewData;
    public readonly ModInfo ModInfo = modInfo;
}
