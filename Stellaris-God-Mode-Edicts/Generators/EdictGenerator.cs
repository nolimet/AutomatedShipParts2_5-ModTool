namespace GodModeEdicts.Generators;

public readonly struct EdictGenerator(StaticEdictGenerator edict)
{
    public override string ToString() => TemplateStorage.Edict.Replace("$$edictName$$", edict.Name).Replace("$$effect$$", edict.GetEffect());

    public static string GenerateFile(Edicts edicts)
    {
        var edictElements = new List<EdictGenerator>();
        for (var i = 0; i < edicts.Length; i++) edictElements.Add(new EdictGenerator(edicts[i]));
        return string.Join("", edictElements);
    }
}
