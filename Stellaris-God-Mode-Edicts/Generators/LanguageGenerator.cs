using System.Collections.Generic;

namespace GodEdictGen.Generators
{
    readonly struct LanguageGenerator
    {
        private readonly StaticEdictGenerator edict;

        private LanguageGenerator(StaticEdictGenerator edict)
        {
            this.edict = edict;
        }

        public override string ToString()
        {
            return
            $"#godEdict_{edict.Name}\n" +
            $"godEdict_{edict.Name}:0 \"£god_Edict_Icon£ {edict.NiceName} \"\n" +
            $"edict_godEdict_{edict.Name}_on:0 \"£god_Edict_Icon£ £trigger_no Enable {edict.NiceName} \"\n" +
            $"edict_godEdict_{edict.Name}_off:0 \"£god_Edict_Icon£ £trigger_yes Disable {edict.NiceName} \"\n" +
            "\n" +
            $"edict_godEdict_{edict.Name}_on_desc:0 \"Enable {edict.NiceName} modifier \"\n" +
            $"edict_godEdict_{edict.Name}_off_desc:0 \"Disable {edict.NiceName} modifier \"\n\n";
        }

        private static string Join(IEnumerable<LanguageGenerator> generators, string langueName)
        {
            return
             $"l_{langueName}:\n\n" +
             string.Join("", generators);
        }

        public static string GenerateFile(Edicts edicts, string langueName)
        {
            var generators = new List<LanguageGenerator>();
            for (int i = 0; i < edicts.Length; i++)
            {
                generators.Add(new LanguageGenerator(edicts[i]));
            }

            return Join(generators, langueName);
        }
    }
}