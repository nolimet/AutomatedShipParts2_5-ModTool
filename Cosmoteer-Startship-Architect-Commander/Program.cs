// See https://aka.ms/new-console-template for more information

using System.Text;
using Cosmoteer.Config;
using Cosmoteer.GameLink;
using Cosmoteer.Writers;
using ModGenLib.Connectors;
using Newtonsoft.Json;
using AnsiConsole = Spectre.Console.AnsiConsole;

if (Console.OutputEncoding.CodePage == 437) // DOS/OEM encoding
    Console.OutputEncoding = Encoding.UTF8;

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
    File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
}

if (args.Length > 0 && args[0] == "--probe-game")
{
    GameProbe.Run(config.BaseGamePath, args[1..]);
    return;
}

if (string.IsNullOrWhiteSpace(config.BaseGamePath) || !Directory.Exists(config.BaseGamePath))
{
    AnsiConsole.MarkupLine($"[red]Path not found at [underline]{config.BaseGamePath}[/][/]");
    return;
}

var partsPath = Path.Combine(config.BaseGamePath ?? throw new NullReferenceException(), "Data", "ships", "terran");
if (!Directory.Exists(partsPath))
    AnsiConsole.WriteLine("NOPE");

// The game's parser is used to read the parts, its assemblies have to be resolvable before any writer runs
if (!GameAssemblyResolver.Register(config.BaseGamePath))
{
    AnsiConsole.MarkupLine("[red]Cosmoteer game assemblies not found, set CosmoteerGamePath in Cosmoteer.Game.props.user and rebuild.[/]");
    return;
}

List<BaseRulesWriter> usedWriters = [];
var vanillaWriter = new VanillaRulesWriter("vanilla/vanilla.rules");

vanillaWriter.Init();
vanillaWriter.WriteVanillaData(config.BaseGamePath);

var connector = new SteamCmdConnector(799600);
await connector.Init();

await Task.Delay(100);
var list = connector.DownloadWorkshopItems(config.Mods);

foreach (var (path, modId) in list)
{
    var modWriter = new ModdedRulesWriter(path, config.BaseGamePath, modId, config.IgnoredParts.GetValueOrDefault(modId), $"mods/{modId}/{modId}.rules");
    await Task.Delay(100);
    usedWriters.Add(modWriter);
    modWriter.Init();
    modWriter.WriteModData();
}

await Task.Delay(100);
var modRulesWriter = new ModRulesWriter();
modRulesWriter.Init();
modRulesWriter.WriteModRules(vanillaWriter, usedWriters, config.ManualMods);

await Task.Delay(100);
await modRulesWriter.DisposeAsync();
foreach (var usedWriter in usedWriters) await usedWriter.DisposeAsync();
await vanillaWriter.DisposeAsync();
