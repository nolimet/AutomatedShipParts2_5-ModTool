// See https://aka.ms/new-console-template for more information

using ModGenerator;
using ModGenerator.Config;
using Newtonsoft.Json;
using AnsiConsole = Spectre.Console.AnsiConsole;

var configPath = Path.Combine(AppContext.BaseDirectory, "ExportConfig.json");
TargetConfiguration config;

if (!File.Exists(configPath))
{
    askForPath:
    var baseGamePath = AnsiConsole.Ask<string>("Please enter the path to your base game:\n");
    if (!Directory.Exists(baseGamePath)) goto askForPath;

    baseGamePath = baseGamePath.Replace('\\', '/').Trim('"');
    config = new TargetConfiguration(baseGamePath);
    File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
}
else
{
    var raw = File.ReadAllText(configPath) ?? throw new InvalidDataException("Json not valid!");
    config = JsonConvert.DeserializeObject<TargetConfiguration>(raw) ?? throw new NullReferenceException("Conversion failed");
}

if (string.IsNullOrWhiteSpace(config.BaseGamePath) || !Directory.Exists(config.BaseGamePath))
{
    AnsiConsole.MarkupLine($"[underline red]Path not found at {config.BaseGamePath}[/]");
    return;
}

var partsPath = Path.Combine(config.BaseGamePath ?? throw new NullReferenceException(), "Data", "ships", "terran");
if (!Directory.Exists(partsPath))
    AnsiConsole.WriteLine("NOPE");

var dataWriter = new DataWriter();
dataWriter.Init();
dataWriter.WriteVanillaData(config.BaseGamePath);

var connector = new SteamCmdConnector();
await connector.Init();

var list = connector.DownloadWorkshopItems(config.Mods);
for (var index = 0; index < list.Count; index++)
{
    var (path, modId) = list[index];
    dataWriter.WriteModData(path, modId);
}

await dataWriter.DisposeAsync();
