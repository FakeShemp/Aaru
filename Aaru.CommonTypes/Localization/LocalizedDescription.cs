using System.ComponentModel;
using Aaru.Localization;

namespace Aaru.CommonTypes;

public class LocalizedDescriptionAttribute : DescriptionAttribute
{
    public LocalizedDescriptionAttribute(string resourceKey) => Description = resourceKey;

    public override string Description => UI.ResourceManager.GetString(field) ?? field;
}