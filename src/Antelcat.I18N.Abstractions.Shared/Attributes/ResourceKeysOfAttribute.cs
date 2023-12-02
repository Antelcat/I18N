using System;

namespace Antelcat.I18N.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ResourceKeysOfAttribute(Type resourceType) : Attribute;
