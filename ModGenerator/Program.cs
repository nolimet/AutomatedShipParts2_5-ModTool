// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
using ModGenerator;
using ModGenerator.Config;
using ModGenerator.Data;
using Newtonsoft.Json;
using Spectre.Console;

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

var destinationsRegex = new Regex(@"^\s*CrewDestinations\s*\[\s*([\s\S]*?)\s*\]", RegexOptions.Multiline);
var locationsRegex = new Regex(@"^\s*CrewLocations\s*\[\s*([\s\S]*?)\s*\]", RegexOptions.Multiline);
var crewCountRegex = new Regex(@"((?:Crew = )(.+?))");
Dictionary<string, CrewData> loadedRules = new();
foreach (var file in Directory.GetFiles(partsPath, "*.rules", SearchOption.AllDirectories))
{
    var data = File.ReadAllText(file);
    var locationsResults = locationsRegex.Match(data);
    var destinationsResults = destinationsRegex.Match(data);
    var crewCountResult = crewCountRegex.Match(data);

    if (locationsResults.Success && destinationsResults.Success && crewCountResult.Success)
        loadedRules[file] = new CrewData(locationsResults.Value, destinationsResults.Value, crewCountResult.Value);
}

Console.WriteLine(loadedRules.First().Value);
Console.WriteLine(TemplateStorage.ActionTemplateModded);
