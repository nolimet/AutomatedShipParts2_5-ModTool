namespace Cosmoteer.GameLink.Extraction;

/// <summary>
///     The game resolves file references like <c>&lt;./Data/ships/terran/base_part_terran.rules&gt;</c> relative to the current working directory,
///     because the game itself always runs from its install folder. Mods inherit from game files this way, so while parsing we pretend to be the game.
///     Restores the previous working directory on dispose.
/// </summary>
public sealed class GameDataScope : IDisposable
{
    private readonly string _previousDirectory;

    public GameDataScope(string gamePath)
    {
        _previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(gamePath);
    }

    public void Dispose() => Directory.SetCurrentDirectory(_previousDirectory);
}
