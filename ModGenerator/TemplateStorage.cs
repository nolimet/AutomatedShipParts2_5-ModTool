using ModGenerator.Helpers;

namespace ModGenerator;

public static class TemplateStorage
{
    public static readonly string ActionTemplateVanilla = TemplateLoadHelper.LoadTemplate(nameof(ActionTemplateVanilla));
    public static readonly string ActionTemplateModded = TemplateLoadHelper.LoadTemplate(nameof(ActionTemplateModded));
    public static readonly string PartTemplateBase = TemplateLoadHelper.LoadTemplate(nameof(PartTemplateBase));
    public static readonly string PartTemplateRepeat = TemplateLoadHelper.LoadTemplate(nameof(PartTemplateRepeat));
    public static readonly string ModRulesBase = TemplateLoadHelper.LoadTemplate(nameof(ModRulesBase));
    public static readonly string ModRuleFileBase = TemplateLoadHelper.LoadTemplate(nameof(ModRuleFileBase));
    public static readonly string PartTemplateFinal = TemplateLoadHelper.LoadTemplate(nameof(PartTemplateFinal));
}
