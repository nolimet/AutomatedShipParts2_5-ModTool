using Cosmoteer.Data;
#if COSMOTEER_LINKED
using Cosmoteer.GameLink.Extraction;
#endif
using Spectre.Console;

namespace Cosmoteer.GameLink;

/// <summary>
///     Where the writers get their part data from. Uses the game's parser (<see cref="Extraction.RulesPartExtractor" />),
///     which only compiles when the game is linked, see Cosmoteer.Game.props.
/// </summary>
public static class PartSource
{
    public static (Dictionary<string, CrewData> parts, Grid report, uint issueCount) GetVanillaParts(string gamePath) =>
#if COSMOTEER_LINKED
        RulesPartExtractor.GetVanillaParts(gamePath);
#else
        throw NotLinked();
#endif

    public static (Dictionary<string, CrewData> parts, Grid report, uint issueCount) GetModParts(string modPath, string gamePath, IReadOnlyList<string>? ignoredParts) =>
#if COSMOTEER_LINKED
        RulesPartExtractor.GetModParts(modPath, gamePath, ignoredParts);
#else
        throw NotLinked();
#endif

#if !COSMOTEER_LINKED
    private static InvalidOperationException NotLinked() =>
        new("The Cosmoteer game assemblies were not linked at build time. Set CosmoteerGamePath in Cosmoteer.Game.props.user and rebuild.");
#endif
}
