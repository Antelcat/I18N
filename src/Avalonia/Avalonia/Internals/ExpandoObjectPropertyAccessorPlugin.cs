using System.Dynamic;
using Avalonia.Data.Core.Plugins;

namespace Antelcat.I18N.Avalonia.Internals;

internal class ExpandoObjectPropertyAccessorPlugin(ExpandoObject target)
    : NotifyInstanceAccessorPlugin<ExpandoObject>(target)
{
    protected override Type GetPropertyType(string propertyName) => typeof(string);

    protected override object? GetValue(ExpandoObject target, string propertyName) =>
        (target as IDictionary<string, object>)[propertyName];

    protected override void OnSubscription(Action<object?> subscription, IPropertyAccessor accessor)
    {
        subscription(accessor.Value);
    }
}