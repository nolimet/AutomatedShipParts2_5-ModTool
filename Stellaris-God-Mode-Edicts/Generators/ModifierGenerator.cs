using System.Globalization;
using System.Text;

namespace GodModeEdicts.Generators;

public readonly struct ModifierGenerator
{
    private static readonly NumberFormatInfo modiferValueFormat = new()
    {
        NumberGroupSeparator = "",
        NumberDecimalSeparator = "."
    };

    private readonly string modifierName;
    private readonly string modifierValueString;
    private readonly double? modifierValue;

    private string ModifierValue
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(modifierValueString))
                return modifierValueString;
            if (modifierValue != null) return ModifierValueDouble.ToString(modiferValueFormat);

            return string.Empty;
        }
    }

    private double ModifierValueDouble => modifierValue ?? 00;

    public ModifierGenerator(string modifierName, string modifierValue)
    {
        this.modifierName = modifierName;
        modifierValueString = modifierValue;
        this.modifierValue = null;
    }

    public ModifierGenerator(string modifierName, double modifierValue)
    {
        this.modifierName = modifierName;
        this.modifierValue = modifierValue;
        modifierValueString = null;
    }

    public static IReadOnlyList<ModifierGenerator> GenerateSet(string modifierFormat, double modifierValue, params string[] modifierNames)
    {
        var sets = new ModifierGenerator[modifierNames.Length];
        for (var i = 0; i < modifierNames.Length; i++) sets[i] = new ModifierGenerator(string.Format(modifierFormat, modifierNames[i]), modifierValue);
        return sets;
    }

    public override string ToString() => $"\t{modifierName} = {ModifierValue}";

    public static string Join(IEnumerable<ModifierGenerator> modifiers)
    {
        var result = new StringBuilder();
        foreach (var modifier in modifiers) result.AppendLine(modifier.ToString());

        return result.ToString().TrimEnd('\n');
    }
}
