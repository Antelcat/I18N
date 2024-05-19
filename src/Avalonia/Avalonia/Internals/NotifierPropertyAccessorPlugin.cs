using System.Dynamic;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Antelcat.I18N.Avalonia.Internals;

internal class NotifierPropertyAccessorPlugin(I18NExtension.ResourceChangedNotifier target)
    : NotifyInstanceAccessorPlugin<I18NExtension.ResourceChangedNotifier>(target)
{
    protected override object? GetValue(I18NExtension.ResourceChangedNotifier target, string propertyName) =>
        target.Source; 

    protected override Type GetPropertyType(string propertyName) => typeof(ExpandoObject);

    protected override void OnPropertyChanged(Action<object?> subscription, object? value)
    {
        subscription(null);
        subscription(value);
    }
}
