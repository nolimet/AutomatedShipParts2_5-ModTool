using System.Collections.Generic;

namespace GodEdictGen.Generators
{
    public readonly struct EventGenerator
    {
        private readonly string edictName;
        private readonly long idOn, idOff;

        private EventGenerator(string edictName, long idOn, long idOff)
        {
            this.edictName = edictName;
            this.idOn = idOn;
            this.idOff = idOff;
        }

        public override string ToString()
        {
            return
            $"## {edictName}\n" +
            "country_event = { \n" +
            "\n" +
            $"  id = godEdict_toggle.{idOn}\n" +
            "   hide_window = yes\n" +
            "   is_triggered_only = yes\n" +
            "\n" +
            "     immediate = {\n" +
            "            root = {\n" +
            "                add_modifier = {\n" +
            $"                    modifier = godEdict_{edictName}\n" +
            "                    days = -1\n" +
            "                }\n" +
            $"                set_country_flag = flag_godEdict_{edictName}_on\n" +
            "          }\n" +
            "     }\n" +
            "}\n" +
            "\n" +
            "country_event = {\n" +
            $"  id = godEdict_toggle.{idOff}\n" +
            "   hide_window = yes\n" +
            "    is_triggered_only = yes\n" +
            "\n" +
            "   immediate = {\n" +
            "       root = {\n" +
            $"            remove_modifier = godEdict_{edictName}\n" +
            $"            remove_country_flag = flag_godEdict_{edictName}_on \n" +
            "        }\n" +
            "   }\n" +
            "}\n\n";
        }

        private static string Join(IEnumerable<EventGenerator> generators)
        {
            return
                "namespace = godEdict_toggle\n" + string.Join("", generators);
        }

        public static string GenerateFile(Edicts edicts)
        {
            var eventElements = new List<EventGenerator>();
            var currentId = 0;
            for (var i = 0; i < edicts.Length; i++)
            {
                eventElements.Add(new EventGenerator(edicts[i].Name, currentId++, currentId++));
            }
            return Join(eventElements);
        }
    }
}