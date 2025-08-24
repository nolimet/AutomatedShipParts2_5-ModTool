using System.Text;

namespace ModGenerator.Writers;

public class ModRulesWriter() : BaseRulesWriter("mod.rules")
{
    protected override bool WriteClosingTag => false;

    public override void Init()
    {
        //Writer.Write(TemplateStorage.ModRulesBase);
    }

    public void WriteModRules(IReadOnlyList<BaseRulesWriter> overrideWriters, IReadOnlyList<ulong> manualMods)
    {
        StringBuilder bldr = new();
        foreach (var manualMod in manualMods)
            bldr.AppendLine($"&<manual/{manualMod}/{manualMod}.rules>/Actions,");

        foreach (var overrideWriter in overrideWriters)
            bldr.AppendLine($"&<{overrideWriter.GetRelativePath()}>/Actions,");

        Writer.Write(FillTemplate(TemplateStorage.ModRulesBase, new Dictionary<string, string> { { "Modules", bldr.ToString() } }));
    }
}
