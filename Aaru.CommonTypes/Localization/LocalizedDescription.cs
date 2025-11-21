using System.ComponentModel;
using Aaru.Localization;

namespace Aaru.CommonTypes;

public class LocalizedDescriptionAttribute : DescriptionAttribute
{
    private readonly string _resourceKey;

    public LocalizedDescriptionAttribute(string resourceKey) => _resourceKey = resourceKey;

    public override string Description => UI.ResourceManager.GetString(_resourceKey) ?? _resourceKey;
}