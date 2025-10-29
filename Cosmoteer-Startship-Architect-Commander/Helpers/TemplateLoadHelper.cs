using System.Reflection;

namespace Cosmoteer.Helpers;

public static class TemplateLoadHelper
{
    public static string LoadTemplate(string name)
    {
        using var stream = GetResourceStream(name);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    public static Stream? GetResourceStream(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        var fullName = asm.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(name));
        if (fullName is null)
        {
            Console.WriteLine($"Failed to find {name}\nAvaiable:\n{string.Join("\n\t", asm.GetManifestResourceNames())}");

            return null;
        }

        return asm.GetManifestResourceStream(fullName);
    }
}
