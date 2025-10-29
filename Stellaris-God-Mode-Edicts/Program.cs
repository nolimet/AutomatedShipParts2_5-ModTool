using GodEdictGen.Generators;
using GodEdictGen.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GodEdictGen
{
    internal class Program
    {
        public static bool DumpFileContent = false;

        private static async Task Main(string[] args)
        {
            if (args != null)
            {
                DumpFileContent = args.Any(x => x == "dumpFileContent");
            }

            await Task.Delay(10);
            try
            {
                await GenerateEdictFile();
            }
            catch (Exception e)
            {
                Console.Write(e);
            }
        }

        private static async Task GenerateEdictFile()
        {
            var edicts = new Edicts();

            string dir = System.IO.Directory.GetCurrentDirectory();
            Console.WriteLine($"PATH : {dir}");
            Console.WriteLine();

            await Task.Delay(100);
            await FileWriter.WriteFileTxt(EdictToggleGenerator.GenerateFile(edicts), "edicts", @"common\edicts");
            await FileWriter.WriteFileTxt(EventGenerator.GenerateFile(edicts), "events", @"events");
            await FileWriter.WriteFileTxt(StaticEdictGenerator.Join(edicts.All), "statics", @"common\static_modifiers");

            await FileWriter.WriteFileYml(LanguageGenerator.GenerateFile(edicts, "english"), "english", @"localisation");

            Console.WriteLine("\nPress any key to exit");
            Console.ReadKey();
            
            Process.Start(dir);
            
            Console.WriteLine("\nExiting program");
            await Task.Delay(500);
        }
    }
}