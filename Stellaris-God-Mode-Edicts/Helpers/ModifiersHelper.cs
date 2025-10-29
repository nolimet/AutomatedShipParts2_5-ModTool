using GodModeEdicts.Generators;

namespace GodModeEdicts.Helpers;

public static class ModifiersHelper
{
    public static IReadOnlyList<ModifierGenerator> AddSet(this IReadOnlyList<ModifierGenerator> orignalList, string modifierFormat, double modifierValue, params string[] modifierNames) => orignalList.Concat(ModifierGenerator.GenerateSet(modifierFormat, modifierValue, modifierNames)).ToArray();
}
