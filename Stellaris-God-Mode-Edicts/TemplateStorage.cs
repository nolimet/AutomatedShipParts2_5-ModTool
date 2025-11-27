using System.Reflection;
using ModGenLib.Helpers;

namespace GodModeEdicts;

public static class TemplateStorage
{
    public static readonly string Edict = TemplateLoadHelper.LoadTemplate(nameof(Edict), Assembly.GetExecutingAssembly());
    public static readonly string EdictLanguage = TemplateLoadHelper.LoadTemplate(nameof(EdictLanguage), Assembly.GetExecutingAssembly());
}
