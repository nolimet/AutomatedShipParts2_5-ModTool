using System.Collections.Generic;

namespace GodEdictGen.Generators
{
    public readonly struct EdictToggleGenerator
    {
        private readonly string edictName;
        private readonly int edictNumberOn, edictNumberOff;

        public EdictToggleGenerator(string editName, int edictNumberOn, int edictNumberOff)
        {
            this.edictName = editName;
            this.edictNumberOn = edictNumberOn;
            this.edictNumberOff = edictNumberOff;
        }

        public override string ToString()
        {
            return
   $"## {edictName}\n" +
   $"godEdict_{edictName}_on = " + "{\n" +
    "	length = 0\n" +
    "	resources = {\n" +
    "		category = edicts\n" +
    "		cost = { influence = 0 }\n" +
    "	}\n" +
    "\n" +
    "	potential = {\n" +
    "			NOT = { has_country_flag = flag_godEdict_" + edictName + "_on }\n" +
    "		}\n" +
    "	\n" +
    "	effect = " +
    "		{\n" +
    "			country_event = { id = godEdict_toggle." + edictNumberOn + " days = 0}\n" +
    "		}\n" +
    "	\n" +
    "	allow = {}\n" +
    "	\n" +
    "	ai_weight = {\n" +
    "			weight = 0\n" +
    "		}\n" +
    "}\n" +
    "\n" +
   $"godEdict_{edictName}_off = " + "{\n" +
    "	length = 0\n" +
    "	\n" +
    "	resources = {\n" +
    "		category = edicts\n" +
    "		cost = { influence = 0 }\n" +
    "	}\n" +
    "	potential = {\n" +
    "			has_country_flag = flag_godEdict_" + edictName + "_on \n" +
    "	}\n" +
    "	\n" +
    "	effect = {\n" +
    "		country_event = { id = godEdict_toggle." + edictNumberOff + " days = 0}\n" +
    "	}\n" +
    "	\n" +
    "	allow = {}\n" +
    "	\n" +
    "	ai_weight = { weight = 0 }\n" +
    "}\n\n";
        }

        public static string GenerateFile(Edicts edicts)
        {
            int k = 0;
            List<EdictToggleGenerator> edictElements = new List<EdictToggleGenerator>();
            for (int i = 0; i < edicts.Length; i++)
            {
                edictElements.Add(new EdictToggleGenerator(edicts[i].Name, k++, k++));
            }
            return string.Join("", edictElements);
        }
    }
}