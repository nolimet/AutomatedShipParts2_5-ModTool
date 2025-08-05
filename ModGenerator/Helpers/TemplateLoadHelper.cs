using System.Reflection;

namespace ModGenerator.Helpers;

public static class TemplateLoadHelper
{
    public static string LoadTemplate(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        var fullName = asm.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(name));
        if (fullName is null)
        {
            Console.WriteLine($"Failed to find {name}\nAvaiable:\n{string.Join("\n\t", asm.GetManifestResourceNames())}");

            return string.Empty;
        }

        using var stream = asm.GetManifestResourceStream(fullName);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }
}
