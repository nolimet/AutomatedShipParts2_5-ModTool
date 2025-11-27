using GodModeEdicts.Generators;
using GodModeEdicts.Helpers;

namespace GodModeEdicts;

internal class Program
{
    public static bool DumpFileContent = false;

    private static void Main(string[] args)
    {
        if (args != null) DumpFileContent = args.Any(x => x == "dumpFileContent");

        var edicts = new Edicts();

        var dir = Directory.GetCurrentDirectory();
        Console.WriteLine($"PATH : {dir}");
        Console.WriteLine();

        FileWriter.WriteFileTxt(EdictGenerator.GenerateFile(edicts), "edicts", @"common\edicts");
        // FileWriter.WriteFileTxt(EdictToggleGenerator.GenerateFile(edicts), "edicts", @"common\edicts");
        // FileWriter.WriteFileTxt(EventGenerator.GenerateFile(edicts), "events", @"events");
        // FileWriter.WriteFileTxt(StaticEdictGenerator.Join(edicts.All), "statics", @"common\static_modifiers");
        //
        // FileWriter.WriteFileYml(ToggleEdictLanguageGenerator.GenerateFile(edicts, "english"), "english", @"localisation");
        FileWriter.WriteFileYml(EdictLanguageGenerator.GenerateFile(edicts, "english"), "english", @"localisation");
    }
}
