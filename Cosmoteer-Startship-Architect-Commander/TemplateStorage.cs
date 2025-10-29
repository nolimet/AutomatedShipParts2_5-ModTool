using System.Reflection;
using ModGenLib.Helpers;

namespace Cosmoteer;

public static class TemplateStorage
{
    public static readonly string ActionTemplateVanilla = TemplateLoadHelper.LoadTemplate(nameof(ActionTemplateVanilla), Assembly.GetExecutingAssembly());
    public static readonly string ActionTemplateModded = TemplateLoadHelper.LoadTemplate(nameof(ActionTemplateModded), Assembly.GetExecutingAssembly());
    public static readonly string PartTemplateBase = TemplateLoadHelper.LoadTemplate(nameof(PartTemplateBase), Assembly.GetExecutingAssembly());
    public static readonly string PartTemplateRepeat = TemplateLoadHelper.LoadTemplate(nameof(PartTemplateRepeat), Assembly.GetExecutingAssembly());
    public static readonly string ModRulesBase = TemplateLoadHelper.LoadTemplate(nameof(ModRulesBase), Assembly.GetExecutingAssembly());
    public static readonly string ModRuleFileBase = TemplateLoadHelper.LoadTemplate(nameof(ModRuleFileBase), Assembly.GetExecutingAssembly());
    public static readonly string PartTemplateFinal = TemplateLoadHelper.LoadTemplate(nameof(PartTemplateFinal), Assembly.GetExecutingAssembly());
}
