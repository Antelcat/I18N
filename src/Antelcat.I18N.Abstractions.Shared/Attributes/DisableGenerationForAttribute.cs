using System;

namespace Antelcat.I18N.Attributes;

/// <summary>
/// attributes excluded in generation
/// </summary>
/// <param name="attributeTypes"></param>
[AttributeUsage(AttributeTargets.Assembly)]
public class DisableGenerationForAttribute(params Type[] attributeTypes) : Attribute;
