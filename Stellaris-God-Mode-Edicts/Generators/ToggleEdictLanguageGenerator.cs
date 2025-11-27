namespace GodModeEdicts.Generators;

internal readonly struct ToggleEdictLanguageGenerator
{
    private readonly StaticEdictGenerator edict;

    private ToggleEdictLanguageGenerator(StaticEdictGenerator edict) => this.edict = edict;

    public override string ToString() =>
        $"#godEdict_{edict.Name}\n" +
        $"godEdict_{edict.Name}:0 \"£god_Edict_Icon£ {edict.NiceName} \"\n" +
        $"edict_godEdict_{edict.Name}_on:0 \"£god_Edict_Icon£ £trigger_no Enable {edict.NiceName} \"\n" +
        $"edict_godEdict_{edict.Name}_off:0 \"£god_Edict_Icon£ £trigger_yes Disable {edict.NiceName} \"\n" +
        "\n" +
        $"edict_godEdict_{edict.Name}_on_desc:0 \"Enable {edict.NiceName} modifier \"\n" +
        $"edict_godEdict_{edict.Name}_off_desc:0 \"Disable {edict.NiceName} modifier \"\n\n";

    private static string Join(IEnumerable<ToggleEdictLanguageGenerator> generators, string langueName) =>
        $"l_{langueName}:\n\n" +
        string.Join("", generators);

    public static string GenerateFile(Edicts edicts, string langueName)
    {
        var generators = new List<ToggleEdictLanguageGenerator>();
        for (var i = 0; i < edicts.Length; i++) generators.Add(new ToggleEdictLanguageGenerator(edicts[i]));

        return Join(generators, langueName);
    }
}
