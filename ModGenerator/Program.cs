// See https://aka.ms/new-console-template for more information

using ModGenerator;
using ModGenerator.Config;
using ModGenerator.Helpers;
using Newtonsoft.Json;
using AnsiConsole = Spectre.Console.AnsiConsole;

var configPath = Path.Combine(AppContext.BaseDirectory, "ExportConfig.json");
TargetConfiguration config;

if (!File.Exists(configPath))
{
    config = new TargetConfiguration();
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
dataWriter.WriteVanillaData(config.BaseGamePath, PartHelper.GetParts(partsPath));

var connector = new SteamCmdConnector();
await connector.Init();

foreach (var modId in config.Mods)
{
    var path = connector.DownloadWorkshopItem(modId);
    dataWriter.WriteModData(path, modId, PartHelper.GetParts(path));
}

await dataWriter.DisposeAsync();
