namespace GodModeEdicts.Generators;

public readonly struct EdictLanguageGenerator(StaticEdictGenerator edict)
{
    public override string ToString() => TemplateStorage.EdictLanguage.Replace("$$edictName$$", edict.Name).Replace("$$edictNiceName$$", edict.NiceName);

    private static string Join(IEnumerable<EdictLanguageGenerator> generators, string langueName) =>
        $"l_{langueName}:\n\n" +
        string.Join("", generators);

    public static string GenerateFile(Edicts edicts, string langueName)
    {
        var generators = new List<EdictLanguageGenerator>();
        for (var i = 0; i < edicts.Length; i++) generators.Add(new EdictLanguageGenerator(edicts[i]));

        return Join(generators, langueName);
    }
}
