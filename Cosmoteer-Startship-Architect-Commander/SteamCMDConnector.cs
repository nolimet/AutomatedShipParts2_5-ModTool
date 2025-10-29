using System.Diagnostics;
using System.Text;
using ModGenerator.Helpers;
using Spectre.Console;

namespace ModGenerator;

public class SteamCmdConnector
{
    private const ulong GameId = 799600;
    private const string DownloadCommand = "workshop_download_item";

    private const string SteamCmdLocalFolder = "steamcmd";
    private const string SteamCmdExecutableName = "steamcmd.exe";
    private const string SteamCmdContentPath = "steamapps/workshop/content";
    private readonly string _steamCmdPath = Path.Combine(AppContext.BaseDirectory, SteamCmdLocalFolder, SteamCmdExecutableName);

    private string? _userName;
    private const string SteamCmdConfigPath = "steamcmd.config";

    public async Task Init()
    {
        if (!File.Exists(_steamCmdPath))
        {
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, SteamCmdLocalFolder));

            var stream = TemplateLoadHelper.GetResourceStream(SteamCmdExecutableName);
            if (stream is null) throw new NullReferenceException("Could not find resource!");
            await using var writer = File.Create(_steamCmdPath);
            await stream.CopyToAsync(writer);
            await stream.FlushAsync();
        }

        var configPath = Path.Combine(AppContext.BaseDirectory, SteamCmdConfigPath);
        if (File.Exists(configPath))
            _userName = Encoding.UTF8.GetString((await File.ReadAllBytesAsync(configPath)).Decrypt());
        else
        {
            _userName = AnsiConsole.Ask<string>("Please enter you steam user name");
            await File.WriteAllBytesAsync(configPath, Encoding.UTF8.GetBytes(_userName).Encrypt());
        }
    }

    public List<(string, ulong)> DownloadWorkshopItems(IReadOnlyCollection<ulong> modIds)
    {
        List<(string path, ulong modId)> items = modIds.Select(x => (Path.Combine(AppContext.BaseDirectory, SteamCmdLocalFolder, SteamCmdContentPath, GameId.ToString(), x.ToString()), x)).ToList();

        var missing = items.Where(x => !Directory.Exists(x.path)).ToArray();
        if (!missing.Any()) return items;

        var modDownloadArg = $"+{DownloadCommand} {GameId} " + string.Join($" +{DownloadCommand} {GameId} ", missing.Select(x => x.modId));
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _steamCmdPath,
                Arguments = $"+login {_userName} {modDownloadArg} +quit", // Example arguments
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            }
        };

        // Start the process
        process.Start();

        // Wait for exit
        process.WaitForExit();

        return items;
    }

    /// <summary>
    /// Downloads a mod
    /// </summary>
    /// <param name="itemId">Id of the workshop item</param>
    /// <returns>Path to the downloaded mod</returns>
    public string DownloadWorkshopItem(ulong itemId)
    {
        var modPath = Path.Combine(AppContext.BaseDirectory, SteamCmdLocalFolder, SteamCmdContentPath, GameId.ToString(), itemId.ToString());
        if (Directory.Exists(modPath)) return modPath;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _steamCmdPath,
                Arguments = $"+login {_userName} +{DownloadCommand} {GameId} {itemId} +quit", // Example arguments
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            }
        };

        // Start the process
        process.Start();

        // Wait for exit
        process.WaitForExit();

        return modPath;
    }
}
