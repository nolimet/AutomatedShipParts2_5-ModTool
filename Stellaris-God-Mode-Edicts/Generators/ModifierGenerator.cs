using System.Globalization;
using System.Text;

namespace GodModeEdicts.Generators;

public readonly struct ModifierGenerator
{
    private static readonly NumberFormatInfo ModiferValueFormat = new()
    {
        NumberGroupSeparator = "",
        NumberDecimalSeparator = "."
    };

    private readonly string _modifierName;
    private readonly string? _modifierValueString;
    private readonly double? _modifierValue;

    private string ModifierValue
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_modifierValueString))
                return _modifierValueString;
            return _modifierValue != null ? ModifierValueDouble.ToString(ModiferValueFormat) : string.Empty;
        }
    }

    private double ModifierValueDouble => _modifierValue ?? 00;

    public ModifierGenerator(string modifierName, string modifierValue)
    {
        _modifierName = modifierName;
        _modifierValueString = modifierValue;
        _modifierValue = null;
    }

    public ModifierGenerator(string modifierName, double modifierValue)
    {
        _modifierName = modifierName;
        _modifierValue = modifierValue;
        _modifierValueString = null;
    }

    public static IReadOnlyList<ModifierGenerator> GenerateSet(string modifierFormat, double modifierValue, params string[] modifierNames)
    {
        var sets = new ModifierGenerator[modifierNames.Length];
        for (var i = 0; i < modifierNames.Length; i++) sets[i] = new ModifierGenerator(string.Format(modifierFormat, modifierNames[i]), modifierValue);
        return sets;
    }

    public override string ToString() => $"\t{_modifierName} = {ModifierValue}";

    public static string Join(IEnumerable<ModifierGenerator> modifiers)
    {
        var result = new StringBuilder();
        foreach (var modifier in modifiers) result.AppendLine(modifier.ToString());

        return result.ToString().TrimEnd('\n');
    }
}
