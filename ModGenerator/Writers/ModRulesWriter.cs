using System.Text;

namespace ModGenerator.Writers;

public class ModRulesWriter() : BaseRulesWriter("mod.rules")
{
    protected override bool WriteClosingTag => false;

    public override void Init() { }

    public void WriteModRules(in BaseRulesWriter vanilla, in IReadOnlyList<BaseRulesWriter> overrideWriters, in IReadOnlyList<ulong> manualMods)
    {
        StringBuilder bldr = new();   
        bldr.AppendLine($"&<{vanilla.GetRelativePath()}>/Actions,");

        foreach (var manualMod in manualMods)
            bldr.AppendLine($"&<manual/{manualMod}/{manualMod}.rules>/Actions,");

        foreach (var overrideWriter in overrideWriters)
            bldr.AppendLine($"&<{overrideWriter.GetRelativePath()}>/Actions,");

        Writer.Write(FillTemplate(TemplateStorage.ModRulesBase, new Dictionary<string, string> {{"Modules", bldr.ToString()}}));
    }
}
