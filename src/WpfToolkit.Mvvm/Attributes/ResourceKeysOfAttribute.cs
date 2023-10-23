using System;

namespace WpfToolkit.Mvvm.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ResourceKeysOfAttribute : Attribute
{
    private readonly Type type;

    public ResourceKeysOfAttribute(Type type)
    {
        this.type = type;
    }
}