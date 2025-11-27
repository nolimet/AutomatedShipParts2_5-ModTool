using System.Text;

namespace GodModeEdicts.Generators;

public class StaticEdictGenerator
{
    public readonly string Name;
    public readonly string NiceName;
    private readonly IReadOnlyList<ModifierGenerator> modifiers;

    public StaticEdictGenerator(string name, IReadOnlyList<ModifierGenerator> modifiers)
    {
        Name = name;
        this.modifiers = modifiers;
        NiceName = name.Replace('_', ' ');
    }

    public StaticEdictGenerator(string name, string niceName, IReadOnlyList<ModifierGenerator> modifiers)
    {
        Name = name;
        this.modifiers = modifiers;
        NiceName = niceName;
    }

    public string GetEffect() => $"{{\n{ModifierGenerator.Join(modifiers)}\n}}";

    public override string ToString() => $"godEdict_{Name} = {GetEffect()}\n";

    public static string Join(IEnumerable<StaticEdictGenerator> edicts)
    {
        var result = new StringBuilder();
        foreach (var edict in edicts) result.AppendLine(edict.ToString());
        return result.ToString();
    }
}
