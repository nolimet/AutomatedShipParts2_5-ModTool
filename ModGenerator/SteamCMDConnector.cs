using System.Diagnostics;
using ModGenerator.Helpers;

namespace ModGenerator;

public class SteamCmdConnector
{
    private const ulong GameId = 799600;
    private const string DownloadCommand = "workshop_download_item";

    private const string SteamCmdLocalFolder = "steamcmd";
    private const string SteamCmdExecutableName = "steamcmd.exe";
    private const string SteamCmdContentPath = "steamapps/workshop/content";
    private readonly string _steamCmdPath = Path.Combine(AppContext.BaseDirectory, SteamCmdLocalFolder, SteamCmdExecutableName);

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
                Arguments = $"+login danio63 +{DownloadCommand} {GameId} {itemId} +quit", // Example arguments
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
