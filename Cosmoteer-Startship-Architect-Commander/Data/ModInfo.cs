namespace Cosmoteer.Data;

public readonly struct ModInfo(string name, string version, string gameVersion)
{
    public readonly string Name = name;
    public readonly string Version = version;
    public readonly string GameVersion = gameVersion;
}
