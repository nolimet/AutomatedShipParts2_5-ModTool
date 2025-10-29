using System.Reflection;

namespace ModGenLib.Helpers;

public static class TemplateLoadHelper
{
    public static string LoadTemplate(string name, Assembly? asm = null)
    {
        using var stream = GetResourceStream(name, asm);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    public static Stream? GetResourceStream(string name, Assembly? asm = null)
    {
        asm ??= Assembly.GetExecutingAssembly();
        var fullName = asm.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(name));
        if (fullName is null)
        {
            Console.WriteLine($"Failed to find {name}\nAvaiable:\n{string.Join("\n\t", asm.GetManifestResourceNames())}");

            return null;
        }

        return asm.GetManifestResourceStream(fullName);
    }
}
