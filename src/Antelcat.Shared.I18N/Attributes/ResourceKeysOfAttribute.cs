using System;

namespace Antelcat.
#if WPF
    Wpf
#elif AVALONIA
    Avalonia
#endif
    .I18N.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ResourceKeysOfAttribute : Attribute
{
    public ResourceKeysOfAttribute(Type resourceType) { }
}